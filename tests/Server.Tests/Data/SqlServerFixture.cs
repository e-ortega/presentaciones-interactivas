using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Server.Infrastructure.Data;
using Testcontainers.MsSql;

namespace Server.Tests.Data;

/// <summary>
/// A real SQL Server with the migrations applied, in a database created for this run.
/// Uses the server in PI_TEST_SQLSERVER when set (e.g. LocalDB), otherwise a container.
/// Without either, the tests that use it are skipped locally; in CI they must run.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private string? _connectionString;
    private string? _unavailableReason;

    public async ValueTask InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable("PI_TEST_SQLSERVER");
        try
        {
            if (server is null)
            {
                _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
                await _container.StartAsync();
                server = _container.GetConnectionString();
            }
        }
        catch (Exception ex) when (Environment.GetEnvironmentVariable("CI") is null)
        {
            _container = null;
            _unavailableReason = $"Docker is not available and PI_TEST_SQLSERVER is not set: {ex.Message}";
            return;
        }

        _connectionString = new SqlConnectionStringBuilder(server)
        {
            InitialCatalog = "pi_tests_" + Guid.NewGuid().ToString("N"),
        }.ConnectionString;

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public AppDbContext CreateContext()
    {
        Assert.SkipWhen(_unavailableReason is not null, _unavailableReason ?? "");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connectionString is not null)
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
