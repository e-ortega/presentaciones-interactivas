using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Server.Features.AdminAccess;
using Server.Features.Import;
using Server.Features.Presentations;
using Server.Infrastructure.Data;
using Server.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        // Azure SQL serverless pauses when idle; the first connection after that needs retries.
        sql => sql.EnableRetryOnFailure()));

// Cookie signing keys live in the database so cookies survive container restarts.
builder.Services.AddDataProtection()
    .SetApplicationName("presentaciones-interactivas")
    .PersistKeysToDbContext<AppDbContext>();

builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddAdminAccess(builder.Configuration);

builder.Services.Configure<ImportOptions>(builder.Configuration.GetSection(ImportOptions.SectionName));
builder.Services.AddSingleton<ISlideRenderer, LibreOfficeSlideRenderer>();
builder.Services.AddSingleton<ImportQueue>();
builder.Services.AddScoped<PresentationImporter>();
builder.Services.AddHostedService<ImportWorker>();

builder.Services.AddSingleton(TimeProvider.System);
// Enums travel as their names ("Ready"), not as numbers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Per IP, to keep the access key from being guessed.
    options.AddPolicy(AdminAccessEndpoints.RateLimitPolicy, http =>
        RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

if (!app.Services.GetRequiredService<AdminKeyVerifier>().IsConfigured)
{
    app.Logger.LogWarning("AdminAccess:KeySha256 is missing or invalid. The teacher area will reject every key.");
}

app.UseExceptionHandler();
app.UseStatusCodePages();

// In production the React build is in wwwroot; in development Vite serves it.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Liveness only: it must not touch the database, or the probes would keep Azure SQL from pausing.
app.MapHealthChecks("/health");

var admin = app.MapGroup("/api/admin").RequireAuthorization(AdminAccessEndpoints.TeacherPolicy);
app.MapAdminAccessEndpoints(admin);
admin.MapPresentationEndpoints(
    app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ImportOptions>>().Value);

// Unknown API routes are a 404, not the React page.
app.Map("/api/{**rest}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound));
app.MapFallbackToFile("index.html");

app.Run();

// Lets the integration tests reference the entry point.
public partial class Program;
