using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Server.Infrastructure.Data;
using Testcontainers.MsSql;

// One SQL Server for the whole test run.
[assembly: AssemblyFixture(typeof(Server.Tests.Data.SqlServerFixture))]

namespace Server.Tests.Data;

/// <summary>
/// A real SQL Server with the migrations applied, in a database created for this run.
/// Uses the server in PI_TEST_SQLSERVER when set (e.g. LocalDB), otherwise a container.
/// Without either, the tests that use it are skipped locally; in CI they must run.
/// It starts on first use, so runs that do not touch the database do not pay for it.
/// </summary>
public sealed class SqlServerFixture : IAsyncDisposable
{
    private readonly Lazy<Task<Database>> _database = new(StartAsync);

    private sealed record Database(string? ConnectionString, MsSqlContainer? Container, string? UnavailableReason);

    /// <summary>Skips the calling test when there is no SQL Server to talk to.</summary>
    public async Task<string> GetConnectionStringAsync()
    {
        var database = await _database.Value;
        Assert.SkipWhen(database.ConnectionString is null, database.UnavailableReason ?? "");
        return database.ConnectionString!;
    }

    public async Task<AppDbContext> CreateContextAsync() => CreateContext(await GetConnectionStringAsync());

    private static AppDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options);

    private static async Task<Database> StartAsync()
    {
        var server = Environment.GetEnvironmentVariable("PI_TEST_SQLSERVER");
        MsSqlContainer? container = null;
        try
        {
            if (server is null)
            {
                container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
                await container.StartAsync();
                server = container.GetConnectionString();
            }
        }
        catch (Exception ex) when (Environment.GetEnvironmentVariable("CI") is null)
        {
            return new Database(null, null, $"Docker is not available and PI_TEST_SQLSERVER is not set: {ex.Message}");
        }

        var connectionString = new SqlConnectionStringBuilder(server)
        {
            InitialCatalog = "pi_tests_" + Guid.NewGuid().ToString("N"),
        }.ConnectionString;

        await using var context = CreateContext(connectionString);
        await context.Database.MigrateAsync();
        return new Database(connectionString, container, null);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_database.IsValueCreated)
        {
            return;
        }

        var database = await _database.Value;
        if (database.ConnectionString is not null)
        {
            await using var context = CreateContext(database.ConnectionString);
            await context.Database.EnsureDeletedAsync();
        }

        if (database.Container is not null)
        {
            await database.Container.DisposeAsync();
        }
    }
}
