using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Domain;
using Server.Infrastructure.Data;

namespace Server.Realtime;

/// <summary>
/// The live session. The server is the source of truth: clients only show what it tells
/// them. A client connects to /hubs/session?sessionId={id}&amp;role={role}, gets a snapshot
/// of the whole state, and from then on receives each change.
/// </summary>
[Authorize(Policy = SessionAccess.MemberPolicy)]
public class SessionHub(AppDbContext db, ILiveSessionStore live, TimeProvider time) : Hub<ISessionClient>
{
    public const string Path = "/hubs/session";

    private const string ConnectionKey = "session-connection";

    /// <summary>What this connection is: resolved once on connecting, trusted afterwards.</summary>
    private sealed record Connection(Guid SessionId, ClientRole Role, Guid? AttendanceId);

    public static string SessionGroup(Guid sessionId) => $"session:{sessionId}";
    public static string PresenterGroup(Guid sessionId) => $"session:{sessionId}:presenter";
    public static string DisplayGroup(Guid sessionId) => $"session:{sessionId}:display";
    private static string AttendanceGroup(Guid attendanceId) => $"attendance:{attendanceId}";

    public override async Task OnConnectedAsync()
    {
        var connection = await ResolveConnectionAsync();
        if (connection is null)
        {
            Context.Abort(); // wrong session, or a role this browser has no right to
            return;
        }

        Context.Items[ConnectionKey] = connection;
        await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroup(connection.SessionId));

        switch (connection.Role)
        {
            case ClientRole.Presenter:
                await Groups.AddToGroupAsync(Context.ConnectionId, PresenterGroup(connection.SessionId));
                break;
            case ClientRole.Display:
                await Groups.AddToGroupAsync(Context.ConnectionId, DisplayGroup(connection.SessionId));
                break;
            case ClientRole.Participant:
                await Groups.AddToGroupAsync(Context.ConnectionId, AttendanceGroup(connection.AttendanceId!.Value));
                live.Connect(connection.SessionId, connection.AttendanceId.Value, Context.ConnectionId);
                await BroadcastAttendanceAsync(connection.SessionId);
                break;
        }

        await Clients.Caller.SessionSnapshot(await BuildSnapshotAsync(connection));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items[ConnectionKey] is Connection { Role: ClientRole.Participant } connection)
        {
            live.Disconnect(connection.SessionId, connection.AttendanceId!.Value, Context.ConnectionId);
            await BroadcastAttendanceAsync(connection.SessionId);
        }
    }

    // --- Presenter -------------------------------------------------------------------------

    public async Task StartSession()
    {
        var session = await LoadSessionAsPresenterAsync();
        if (session.Status != SessionStatus.Lobby)
        {
            throw new HubException(HubErrors.InvalidState);
        }

        session.Start(time.GetUtcNow());
        await db.SaveChangesAsync();
        await Clients.Group(SessionGroup(session.Id)).SessionStarted(session.CurrentSlideIndex, session.CurrentStepIndex);
    }

    /// <summary>Always a slide and a step (D-26), even while every slide has a single step.</summary>
    public async Task GoTo(int slideIndex, int stepIndex)
    {
        var session = await LoadSessionAsPresenterAsync();
        if (session.Status != SessionStatus.Live)
        {
            throw new HubException(HubErrors.InvalidState);
        }

        var stepCounts = await StepCountsAsync(session.PresentationId);
        if (slideIndex < 0 || slideIndex >= stepCounts.Count || stepIndex < 0 || stepIndex >= stepCounts[slideIndex])
        {
            throw new HubException(HubErrors.InvalidPosition);
        }

        session.GoTo(slideIndex, stepIndex);
        await db.SaveChangesAsync(); // durable at once: a restart resumes on this slide
        await Clients.Group(SessionGroup(session.Id)).SlideChanged(slideIndex, stepIndex);
    }

    public async Task EndSession()
    {
        var session = await LoadSessionAsPresenterAsync();
        if (session.Status == SessionStatus.Ended)
        {
            throw new HubException(HubErrors.InvalidState);
        }

        session.End(time.GetUtcNow());
        await db.SaveChangesAsync();
        await Clients.Group(SessionGroup(session.Id)).SessionEnded();
        live.Forget(session.Id);
    }

    public async Task RemoveParticipant(Guid attendanceId)
    {
        var session = await LoadSessionAsPresenterAsync();
        var attendance = await db.Attendances.FirstOrDefaultAsync(a => a.Id == attendanceId && a.SessionId == session.Id)
            ?? throw new HubException(HubErrors.NotFound);

        attendance.Remove();
        await db.SaveChangesAsync();

        await Clients.Group(AttendanceGroup(attendanceId)).RemovedFromSession();
        // A well-behaved client leaves on that message; one that does not stops hearing the session anyway.
        foreach (var connectionId in live.Remove(session.Id, attendanceId))
        {
            await Groups.RemoveFromGroupAsync(connectionId, SessionGroup(session.Id));
        }

        await BroadcastAttendanceAsync(session.Id);
    }

    // --- Helpers ---------------------------------------------------------------------------

    /// <summary>Reads the session and role the client asked for, and checks its cookies allow them.</summary>
    private async Task<Connection?> ResolveConnectionAsync()
    {
        var query = Context.GetHttpContext()?.Request.Query;
        if (query is null
            || !Guid.TryParse(query["sessionId"], out var sessionId)
            || !Enum.TryParse<ClientRole>(query["role"], ignoreCase: false, out var role)
            || !Enum.IsDefined(role)
            || Context.User is not { } user
            || !await db.Sessions.AnyAsync(s => s.Id == sessionId))
        {
            return null;
        }

        switch (role)
        {
            case ClientRole.Presenter when SessionAccess.IsTeacher(user):
            case ClientRole.Display when SessionAccess.IsDisplayOf(user, sessionId):
                return new Connection(sessionId, role, AttendanceId: null);

            case ClientRole.Participant when SessionAccess.AttendanceIn(user, sessionId) is { } attendanceId:
                var admitted = await db.Attendances.AnyAsync(a => a.Id == attendanceId && a.SessionId == sessionId && a.Admitted);
                return admitted ? new Connection(sessionId, role, attendanceId) : null;

            default:
                return null;
        }
    }

    private async Task<Session> LoadSessionAsPresenterAsync()
    {
        if (Context.Items[ConnectionKey] is not Connection { Role: ClientRole.Presenter } connection)
        {
            throw new HubException(HubErrors.NotAllowed);
        }

        return await db.Sessions.FirstOrDefaultAsync(s => s.Id == connection.SessionId)
            ?? throw new HubException(HubErrors.NotFound);
    }

    private async Task<SessionSnapshot> BuildSnapshotAsync(Connection connection)
    {
        var session = await db.Sessions.AsNoTracking().FirstAsync(s => s.Id == connection.SessionId);
        var title = await db.Presentations.Where(p => p.Id == session.PresentationId).Select(p => p.Title).FirstAsync();
        var isParticipant = connection.Role == ClientRole.Participant;

        return new SessionSnapshot(
            session.Id,
            session.Status,
            title,
            session.CurrentSlideIndex,
            session.CurrentStepIndex,
            await StepCountsAsync(session.PresentationId),
            Code: isParticipant ? null : session.Code,
            Attendance: isParticipant ? null : await BuildAttendanceAsync(session.Id, withNames: connection.Role == ClientRole.Presenter));
    }

    private async Task<IReadOnlyList<int>> StepCountsAsync(Guid presentationId) =>
        await db.Slides
            .Where(s => s.PresentationId == presentationId)
            .OrderBy(s => s.Index)
            .Select(s => s.Steps.Count)
            .ToListAsync();

    private async Task BroadcastAttendanceAsync(Guid sessionId)
    {
        await Clients.Group(PresenterGroup(sessionId)).AttendanceChanged(await BuildAttendanceAsync(sessionId, withNames: true));
        await Clients.Group(DisplayGroup(sessionId)).AttendanceChanged(await BuildAttendanceAsync(sessionId, withNames: false));
    }

    private async Task<AttendanceUpdate> BuildAttendanceAsync(Guid sessionId, bool withNames)
    {
        var connected = live.ConnectedAttendances(sessionId);
        if (!withNames)
        {
            return new AttendanceUpdate(connected.Count, Attendees: null);
        }

        var attendees = await db.Attendances
            .Where(a => a.SessionId == sessionId && a.Admitted)
            .OrderBy(a => a.JoinedAt)
            .Select(a => new { a.Id, a.DisplayName })
            .ToListAsync();

        return new AttendanceUpdate(
            connected.Count,
            [.. attendees.Select(a => new Attendee(a.Id, a.DisplayName, connected.Contains(a.Id)))]);
    }
}
