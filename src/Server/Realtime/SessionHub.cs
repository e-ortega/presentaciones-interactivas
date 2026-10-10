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

    /// <summary>
    /// A refused connection is closed with a HubException: its message (see <see cref="HubErrors"/>)
    /// reaches the client, which can then say "you were removed" instead of retrying forever.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var (connection, status) = await ResolveConnectionAsync();
        Context.Items[ConnectionKey] = connection;

        // An ended session has nothing left to follow: its former members get the snapshot
        // that says so, and are not registered anywhere. The teacher can still look at it.
        var follows = status != SessionStatus.Ended || connection.Role == ClientRole.Presenter;
        if (follows)
        {
            await JoinGroupsAsync(connection);
        }

        await Clients.Caller.SessionSnapshot(await BuildSnapshotAsync(connection));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (CurrentConnection() is { Role: ClientRole.Participant } connection)
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

        var stepCounts = await db.Slides
            .Where(s => s.PresentationId == session.PresentationId)
            .OrderBy(s => s.Index)
            .Select(s => s.Steps.Count)
            .ToListAsync();
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

    private Connection? CurrentConnection() =>
        Context.Items.TryGetValue(ConnectionKey, out var value) ? value as Connection : null;

    /// <summary>
    /// Reads the session and role the client asked for and checks its cookies allow them.
    /// Throws a HubException with the reason when they do not.
    /// </summary>
    private async Task<(Connection Connection, SessionStatus Status)> ResolveConnectionAsync()
    {
        var query = Context.GetHttpContext()?.Request.Query;
        if (query is null
            || !Guid.TryParse(query["sessionId"], out var sessionId)
            || !Enum.TryParse<ClientRole>(query["role"], ignoreCase: false, out var role)
            || !Enum.IsDefined(role)
            || Context.User is not { } user)
        {
            throw new HubException(HubErrors.NotAllowed);
        }

        // The same answer for "no such session" and "not yours": no hint about which exist.
        var status = await db.Sessions.Where(s => s.Id == sessionId).Select(s => (SessionStatus?)s.Status).FirstOrDefaultAsync()
            ?? throw new HubException(HubErrors.NotAllowed);

        switch (role)
        {
            case ClientRole.Presenter when SessionAccess.IsTeacher(user):
            case ClientRole.Display when SessionAccess.IsDisplayOf(user, sessionId):
                return (new Connection(sessionId, role, AttendanceId: null), status);

            case ClientRole.Participant when SessionAccess.AttendanceIn(user, sessionId) is { } attendanceId:
                if (!await IsAdmittedAsync(sessionId, attendanceId))
                {
                    throw new HubException(HubErrors.Removed);
                }

                return (new Connection(sessionId, role, attendanceId), status);

            default:
                throw new HubException(HubErrors.NotAllowed);
        }
    }

    private async Task JoinGroupsAsync(Connection connection)
    {
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
                var attendanceId = connection.AttendanceId!.Value;
                await Groups.AddToGroupAsync(Context.ConnectionId, AttendanceGroup(attendanceId));
                live.Connect(connection.SessionId, attendanceId, Context.ConnectionId);

                // The teacher may have removed this participant between the check on connecting
                // and the line above. From here on the store knows this connection, so a removal
                // finds it; a removal that already happened is caught by looking once more.
                if (!await IsAdmittedAsync(connection.SessionId, attendanceId))
                {
                    live.Disconnect(connection.SessionId, attendanceId, Context.ConnectionId);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroup(connection.SessionId));
                    // The removal may have counted this connection a moment ago: send the count again.
                    await BroadcastAttendanceAsync(connection.SessionId);
                    throw new HubException(HubErrors.Removed);
                }

                await BroadcastAttendanceAsync(connection.SessionId);
                break;
        }
    }

    private Task<bool> IsAdmittedAsync(Guid sessionId, Guid attendanceId) =>
        db.Attendances.AnyAsync(a => a.Id == attendanceId && a.SessionId == sessionId && a.Admitted);

    private async Task<Session> LoadSessionAsPresenterAsync()
    {
        if (CurrentConnection() is not { Role: ClientRole.Presenter } connection)
        {
            throw new HubException(HubErrors.NotAllowed);
        }

        return await db.Sessions.FirstOrDefaultAsync(s => s.Id == connection.SessionId)
            ?? throw new HubException(HubErrors.NotFound);
    }

    private async Task<SessionSnapshot> BuildSnapshotAsync(Connection connection)
    {
        var session = await db.Sessions
            .Where(s => s.Id == connection.SessionId)
            .Select(s => new { s.Status, s.CurrentSlideIndex, s.CurrentStepIndex, s.Code, s.PresentationId })
            .FirstAsync();
        var presentation = await db.Presentations
            .Where(p => p.Id == session.PresentationId)
            .Select(p => new { p.Title, StepCounts = p.Slides.OrderBy(s => s.Index).Select(s => s.Steps.Count).ToList() })
            .FirstAsync();

        // Students are already in and need neither. Once the session ended its code is free for
        // another session, so only the teacher still sees it.
        var isPresenter = connection.Role == ClientRole.Presenter;
        var seesLobbyData = isPresenter || (connection.Role == ClientRole.Display && session.Status != SessionStatus.Ended);

        return new SessionSnapshot(
            connection.SessionId,
            session.Status,
            presentation.Title,
            session.CurrentSlideIndex,
            session.CurrentStepIndex,
            presentation.StepCounts,
            Code: seesLobbyData ? session.Code : null,
            Attendance: seesLobbyData ? await BuildAttendanceAsync(connection.SessionId) : null);
    }

    private async Task BroadcastAttendanceAsync(Guid sessionId)
    {
        var attendance = await BuildAttendanceAsync(sessionId);
        await Clients.Groups(PresenterGroup(sessionId), DisplayGroup(sessionId)).AttendanceChanged(attendance);
    }

    private async Task<AttendanceUpdate> BuildAttendanceAsync(Guid sessionId)
    {
        var connected = live.ConnectedAttendances(sessionId);
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
