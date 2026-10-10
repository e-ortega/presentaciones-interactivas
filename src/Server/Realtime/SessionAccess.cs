using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Server.Features.AdminAccess;

namespace Server.Realtime;

/// <summary>
/// Who may be in a session, and as what. Three signed cookies, one per kind of client:
/// the teacher's (see AdminAccess), a participant's and a display window's.
/// The same cookies authenticate the REST calls and the SignalR connection, so a page
/// reload or a dropped connection comes back as the same person.
/// </summary>
public static class SessionAccess
{
    public const string ParticipantScheme = "ParticipantCookie";
    public const string DisplayScheme = "DisplayCookie";

    /// <summary>Signed in as teacher, participant or display. Which session is checked per request.</summary>
    public const string MemberPolicy = "SessionMember";

    private const string SessionIdClaim = "session_id";
    private const string AttendanceIdClaim = "attendance_id";

    public static IServiceCollection AddSessionAccess(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddCookie(ParticipantScheme, options => Configure(options, "pi_participant"))
            .AddCookie(DisplayScheme, options => Configure(options, "pi_display"));

        services.AddAuthorizationBuilder()
            .AddPolicy(MemberPolicy, policy => policy
                .AddAuthenticationSchemes(AdminAccessEndpoints.TeacherScheme, ParticipantScheme, DisplayScheme)
                .RequireAuthenticatedUser());

        return services;
    }

    public static Task SignInParticipantAsync(HttpContext http, Guid sessionId, Guid attendanceId) =>
        http.SignInAsync(ParticipantScheme, Principal(ParticipantScheme, sessionId, attendanceId));

    public static Task SignInDisplayAsync(HttpContext http, Guid sessionId) =>
        http.SignInAsync(DisplayScheme, Principal(DisplayScheme, sessionId, attendanceId: null));

    public static bool IsTeacher(ClaimsPrincipal user) =>
        user.Identities.Any(identity => identity is { IsAuthenticated: true, AuthenticationType: AdminAccessEndpoints.TeacherScheme });

    /// <summary>The teacher, or a display window opened for exactly this session.</summary>
    public static bool IsDisplayOf(ClaimsPrincipal user, Guid sessionId) =>
        IsTeacher(user) || IdentityFor(user, DisplayScheme, sessionId) is not null;

    /// <summary>
    /// The attendance this browser already joined the session with, read from its participant
    /// cookie. For endpoints that anyone may call, where that cookie is not read automatically.
    /// </summary>
    public static async Task<Guid?> ParticipantAttendanceAsync(HttpContext http, Guid sessionId)
    {
        var result = await http.AuthenticateAsync(ParticipantScheme);
        return result.Principal is { } principal ? AttendanceIn(principal, sessionId) : null;
    }

    /// <summary>The attendance this browser joined the session with, if it did.</summary>
    public static Guid? AttendanceIn(ClaimsPrincipal user, Guid sessionId) =>
        Guid.TryParse(IdentityFor(user, ParticipantScheme, sessionId)?.FindFirst(AttendanceIdClaim)?.Value, out var attendanceId)
            ? attendanceId
            : null;

    private static ClaimsIdentity? IdentityFor(ClaimsPrincipal user, string scheme, Guid sessionId) =>
        user.Identities.FirstOrDefault(identity =>
            identity.IsAuthenticated
            && identity.AuthenticationType == scheme
            && identity.FindFirst(SessionIdClaim)?.Value == sessionId.ToString());

    private static ClaimsPrincipal Principal(string scheme, Guid sessionId, Guid? attendanceId)
    {
        var claims = new List<Claim> { new(SessionIdClaim, sessionId.ToString()) };
        if (attendanceId is not null)
        {
            claims.Add(new Claim(AttendanceIdClaim, attendanceId.Value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
    }

    private static void Configure(CookieAuthenticationOptions options, string cookieName)
    {
        options.Cookie.Name = cookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Long enough for the longest class day; the session ending is what really closes the door.
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        // An API answers 401/403; it does not redirect to a login page.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    }
}
