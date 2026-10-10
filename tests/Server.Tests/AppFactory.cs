using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Server.Features.Import;

namespace Server.Tests;

/// <summary>
/// Runs the real app in memory, without a database: nothing these tests call touches it.
/// Tests that need SQL Server use <see cref="Data.SqlServerFixture"/> instead.
/// </summary>
public class AppFactory : WebApplicationFactory<Program>
{
    public const string AccessKey = "test-access-key";

    public static string AccessKeyHash => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(AccessKey)));

    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "pi-tests-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("AdminAccess:KeySha256", AccessKeyHash);
        builder.UseSetting("ConnectionStrings:Default", "Server=unused;Database=unused");
        builder.UseSetting("Storage:LocalRoot", _storageRoot);

        builder.ConfigureTestServices(services =>
        {
            // The app keeps cookie keys in the database; here they stay in memory.
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            // The import worker looks for unfinished imports in the database at startup.
            var worker = services.Single(service => service.ImplementationType == typeof(ImportWorker));
            services.Remove(worker);
        });
    }

    /// <summary>A client over https that has already entered the access key.</summary>
    public static async Task<HttpClient> SignInAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/access", new { key = AccessKey }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>A client over https, so it keeps and sends the Secure teacher cookie.</summary>
    public HttpClient CreateHttpsClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }
}
