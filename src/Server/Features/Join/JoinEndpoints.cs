using Microsoft.EntityFrameworkCore;
using Server.Domain;
using Server.Features.Sessions;
using Server.Infrastructure.Data;
using Server.Realtime;

namespace Server.Features.Join;

/// <param name="Title">The presentation, so the student sees they have the right code before giving their data.</param>
/// <param name="NoticeVersion">Version of the privacy notice the form shows and the student accepts.</param>
public record JoinInfo(string Title, string NoticeVersion);

/// <param name="Consent">True only if the student ticked the consent box, which starts unticked (Ley 8968).</param>
public record JoinRequest(string? Name, string? Email, bool Consent);

public record JoinResult(Guid SessionId);

public enum JoinError
{
    SessionNotFound,
    NameRequired,
    NameTooLong,
    EmailInvalid,
    ConsentRequired,

    /// <summary>The teacher removed this participant from the session.</summary>
    Removed,
}

/// <summary>How a student gets into a session: code, then name, email and consent (D-15, D-29).</summary>
public static class JoinEndpoints
{
    public const string RateLimitPolicy = "join";

    /// <summary>Version of docs/legal/aviso-privacidad.md. Change it when the notice changes.</summary>
    public const string NoticeVersion = "1.0";

    public static void MapJoinEndpoints(this IEndpointRouteBuilder app)
    {
        var join = app.MapGroup("/api/join").RequireRateLimiting(RateLimitPolicy);

        join.MapGet("/{code}", GetInfo)
            .Produces<JoinInfo>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        join.MapPost("/{code}", Join)
            .Produces<JoinResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetInfo(string code, AppDbContext db, CancellationToken cancellationToken)
    {
        var session = await FindJoinableSessionAsync(db, code, cancellationToken);
        if (session is null)
        {
            return Problem(JoinError.SessionNotFound);
        }

        var title = await db.Presentations.Where(p => p.Id == session.PresentationId).Select(p => p.Title).FirstAsync(cancellationToken);
        return Results.Ok(new JoinInfo(title, NoticeVersion));
    }

    private static async Task<IResult> Join(
        string code, JoinRequest request, AppDbContext db, TimeProvider time, HttpContext http, CancellationToken cancellationToken)
    {
        var session = await FindJoinableSessionAsync(db, code, cancellationToken);
        if (session is null)
        {
            return Problem(JoinError.SessionNotFound);
        }

        var name = request.Name?.Trim() ?? "";
        if (name.Length == 0)
        {
            return Problem(JoinError.NameRequired);
        }

        if (name.Length > Participant.MaxNameLength)
        {
            return Problem(JoinError.NameTooLong);
        }

        if (Participant.NormalizeEmail(request.Email) is not { } email)
        {
            return Problem(JoinError.EmailInvalid);
        }

        if (!request.Consent)
        {
            return Problem(JoinError.ConsentRequired);
        }

        var participant = await FindOrCreateParticipantAsync(db, email, name, cancellationToken);
        var attendance = await db.Attendances
            .FirstOrDefaultAsync(a => a.SessionId == session.Id && a.ParticipantId == participant.Id, cancellationToken);

        if (attendance is null)
        {
            attendance = new Attendance(session.Id, participant.Id, name, consentedAt: time.GetUtcNow(), NoticeVersion);
            db.Attendances.Add(attendance);
        }
        else if (!attendance.Admitted)
        {
            return Problem(JoinError.Removed);
        }
        else
        {
            attendance.Rename(name); // same person coming back, perhaps from another device
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (db.Entry(attendance).State == EntityState.Added)
        {
            // The form was sent twice at once: the unique index kept one attendance. Use that one.
            db.Entry(attendance).State = EntityState.Detached;
            attendance = await db.Attendances
                .FirstAsync(a => a.SessionId == session.Id && a.ParticipantId == participant.Id, cancellationToken);
        }

        await SessionAccess.SignInParticipantAsync(http, session.Id, attendance.Id);
        return Results.Ok(new JoinResult(session.Id));
    }

    private static async Task<Session?> FindJoinableSessionAsync(AppDbContext db, string code, CancellationToken cancellationToken)
    {
        if (SessionCode.Normalize(code) is not { } normalized)
        {
            return null;
        }

        // A code stops working when its session ends, and may later be reused by another one.
        return await db.Sessions.FirstOrDefaultAsync(s => s.Code == normalized && s.Status != SessionStatus.Ended, cancellationToken);
    }

    /// <summary>The email is the identity (D-16): the same email is always the same participant.</summary>
    private static async Task<Participant> FindOrCreateParticipantAsync(
        AppDbContext db, string email, string name, CancellationToken cancellationToken)
    {
        var participant = await db.Participants.FirstOrDefaultAsync(p => p.Email == email, cancellationToken);
        if (participant is not null)
        {
            return participant;
        }

        participant = new Participant(email, name);
        db.Participants.Add(participant);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return participant;
        }
        catch (DbUpdateException)
        {
            // Two requests with the same new email at once: the unique index let one through.
            db.Entry(participant).State = EntityState.Detached;
            return await db.Participants.FirstAsync(p => p.Email == email, cancellationToken);
        }
    }

    private static IResult Problem(JoinError error) =>
        Results.Problem(
            statusCode: error switch
            {
                JoinError.SessionNotFound => StatusCodes.Status404NotFound,
                JoinError.Removed => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest,
            },
            title: "Could not join the session.",
            extensions: new Dictionary<string, object?> { [SessionEndpoints.CodeErrorKey] = error.ToString() });
}
