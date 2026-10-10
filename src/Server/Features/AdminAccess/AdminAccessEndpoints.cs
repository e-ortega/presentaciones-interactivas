using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Server.Features.AdminAccess;

public static class AdminAccessEndpoints
{
    public const string TeacherPolicy = "Teacher";
    public const string TeacherScheme = "TeacherCookie";
    public const string RateLimitPolicy = "admin-access";

    public record AccessRequest(string? Key);

    /// <summary>
    /// POST /api/access exchanges the key for a cookie; GET /api/admin/access tells the
    /// frontend whether the cookie is still valid; DELETE /api/admin/access signs out.
    /// </summary>
    public static void MapAdminAccessEndpoints(this IEndpointRouteBuilder app, RouteGroupBuilder admin)
    {
        app.MapPost("/api/access", SignIn).RequireRateLimiting(RateLimitPolicy);

        admin.MapGet("/access", () => Results.NoContent());
        admin.MapDelete("/access", async (HttpContext http) =>
        {
            await http.SignOutAsync(TeacherScheme);
            return Results.NoContent();
        });
    }

    private static async Task<IResult> SignIn(AccessRequest request, AdminKeyVerifier verifier, HttpContext http)
    {
        if (!verifier.Verify(request.Key))
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid access key.");
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, TeacherPolicy)], TeacherScheme);
        await http.SignInAsync(TeacherScheme, new ClaimsPrincipal(identity));
        return Results.NoContent();
    }

    public static IServiceCollection AddAdminAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminAccessOptions>(configuration.GetSection(AdminAccessOptions.SectionName));
        services.AddSingleton<AdminKeyVerifier>();

        services.AddAuthentication(TeacherScheme)
            .AddCookie(TeacherScheme, options =>
            {
                options.Cookie.Name = "pi_teacher";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
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
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(TeacherPolicy, policy => policy
                .AddAuthenticationSchemes(TeacherScheme)
                .RequireRole(TeacherPolicy));

        return services;
    }
}
