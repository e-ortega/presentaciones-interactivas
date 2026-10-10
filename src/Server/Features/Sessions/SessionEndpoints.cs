using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Server.Domain;
using Server.Features.Presentations;
using Server.Infrastructure.Data;
using Server.Infrastructure.Storage;
using Server.Realtime;

namespace Server.Features.Sessions;

public record CreateSessionRequest(Guid PresentationId);

/// <param name="Code">8 digits. The interface shows them as 8888-6666.</param>
public record SessionInfo(Guid Id, string Code, SessionStatus Status, Guid PresentationId, string Title);

/// <summary>What the presenter view needs that never travels through the hub: the speaker notes.</summary>
public record SessionDetail(
    Guid Id,
    string Code,
    SessionStatus Status,
    int SlideIndex,
    int StepIndex,
    Guid PresentationId,
    string Title,
    IReadOnlyList<SlideDetail> Slides);

public record DisplayToken(string Token);

public record RedeemDisplayTokenRequest(string? Token);

public record DisplayAccess(Guid SessionId);

public static class SessionEndpoints
{
    public const string CodeErrorKey = "code";

    public static void MapSessionEndpoints(this IEndpointRouteBuilder app, RouteGroupBuilder admin)
    {
        var sessions = admin.MapGroup("/sessions");
        sessions.MapPost("/", Create)
            .Produces<SessionInfo>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        sessions.MapGet("/{id:guid}", Get)
            .Produces<SessionDetail>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        sessions.MapPost("/{id:guid}/display-token", CreateDisplayToken)
            .Produces<DisplayToken>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost("/api/display/access", RedeemDisplayToken)
            .RequireRateLimiting(Join.JoinEndpoints.RateLimitPolicy)
            .Produces<DisplayAccess>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // Slide images for everyone in the session: teacher, display window and students.
        app.MapGet("/api/sessions/{id:guid}/slides/{slideIndex:int}/steps/{stepIndex:int}", GetStepImage)
            .RequireAuthorization(SessionAccess.MemberPolicy);
    }

    private static async Task<IResult> Create(
        CreateSessionRequest request, AppDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        var presentation = await db.Presentations
            .Where(p => p.Id == request.PresentationId)
            .Select(p => new { p.Id, p.Title, p.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (presentation is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound);
        }

        if (presentation.Status != ImportStatus.Ready)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "The presentation is not ready to be presented.",
                extensions: new Dictionary<string, object?> { [CodeErrorKey] = "PresentationNotReady" });
        }

        // The code is random; a unique index on sessions that have not ended catches the rare clash.
        for (var attempt = 1; ; attempt++)
        {
            var session = new Session(presentation.Id, SessionCode.Generate(), time.GetUtcNow());
            db.Sessions.Add(session);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return Results.Created(
                    $"/api/admin/sessions/{session.Id}",
                    new SessionInfo(session.Id, session.Code, session.Status, presentation.Id, presentation.Title));
            }
            catch (DbUpdateException) when (attempt < 5)
            {
                db.Entry(session).State = EntityState.Detached;
            }
        }
    }

    private static async Task<IResult> Get(Guid id, AppDbContext db, CancellationToken cancellationToken)
    {
        var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound);
        }

        var presentation = await db.Presentations
            .Where(p => p.Id == session.PresentationId)
            .Select(p => new
            {
                p.Title,
                Slides = p.Slides.OrderBy(s => s.Index).Select(s => new SlideDetail(s.Index, s.SpeakerNotes, s.Steps.Count)).ToList(),
            })
            .FirstAsync(cancellationToken);

        return Results.Ok(new SessionDetail(
            session.Id,
            session.Code,
            session.Status,
            session.CurrentSlideIndex,
            session.CurrentStepIndex,
            session.PresentationId,
            presentation.Title,
            presentation.Slides));
    }

    private static async Task<IResult> CreateDisplayToken(
        Guid id, AppDbContext db, DisplayTokenStore tokens, CancellationToken cancellationToken) =>
        await db.Sessions.AnyAsync(s => s.Id == id, cancellationToken)
            ? Results.Ok(new DisplayToken(tokens.Create(id)))
            : Results.Problem(statusCode: StatusCodes.Status404NotFound);

    private static async Task<IResult> RedeemDisplayToken(
        RedeemDisplayTokenRequest request, DisplayTokenStore tokens, HttpContext http)
    {
        if (tokens.Redeem(request.Token) is not { } sessionId)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "The link to this window is no longer valid.",
                extensions: new Dictionary<string, object?> { [CodeErrorKey] = "InvalidDisplayToken" });
        }

        await SessionAccess.SignInDisplayAsync(http, sessionId);
        return Results.Ok(new DisplayAccess(sessionId));
    }

    private static async Task<IResult> GetStepImage(
        Guid id, int slideIndex, int stepIndex, AppDbContext db, IFileStorage storage, HttpContext http, CancellationToken cancellationToken)
    {
        var session = await db.Sessions
            .Where(s => s.Id == id)
            .Select(s => new { s.PresentationId, s.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null || !await MaySeeSlidesAsync(http, db, id, session.Status, cancellationToken))
        {
            // The same answer for "no such session" and "not yours": no hint about which exist.
            return Results.Problem(statusCode: StatusCodes.Status404NotFound);
        }

        var imagePath = await db.Slides
            .Where(s => s.PresentationId == session.PresentationId && s.Index == slideIndex)
            .SelectMany(s => s.Steps)
            .Where(step => step.Index == stepIndex)
            .Select(step => step.ImagePath)
            .FirstOrDefaultAsync(cancellationToken);

        var stream = imagePath is null ? null : await storage.OpenReadAsync(imagePath, cancellationToken);
        if (stream is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound);
        }

        // "private": only this browser keeps it. Not "immutable" here: who may see it can change.
        http.Response.Headers.CacheControl = "private, max-age=3600";
        return Results.Stream(stream, "image/png");
    }

    /// <summary>
    /// The teacher always; the display window of this session; and a student who joined it and
    /// was not removed, only while the class is live.
    /// </summary>
    private static async Task<bool> MaySeeSlidesAsync(
        HttpContext http, AppDbContext db, Guid sessionId, SessionStatus status, CancellationToken cancellationToken)
    {
        if (SessionAccess.IsDisplayOf(http.User, sessionId)) // includes the teacher
        {
            return true;
        }

        return status == SessionStatus.Live
            && SessionAccess.AttendanceIn(http.User, sessionId) is { } attendanceId
            && await db.Attendances.AnyAsync(a => a.Id == attendanceId && a.SessionId == sessionId && a.Admitted, cancellationToken);
    }
}

public static class SessionCode
{
    public const int Length = 8;

    /// <summary>8 random digits from the cryptographic generator: it is what lets people in.</summary>
    public static string Generate() =>
        RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// The code as stored, from what someone typed: "8888-6666", "8888 6666" or "88886666".
    /// Null when it is not 8 digits.
    /// </summary>
    public static string? Normalize(string? typed)
    {
        var digits = new string([.. (typed ?? "").Where(c => c is not ('-' or ' '))]);
        return digits.Length == Length && digits.All(char.IsAsciiDigit) ? digits : null;
    }
}
