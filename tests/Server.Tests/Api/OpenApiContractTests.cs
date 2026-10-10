using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Server.Tests.Api;

/// <summary>
/// The API contract lives in src/web/src/api/openapi.json, and the TypeScript types are
/// generated from that file. This test fails when the server no longer matches it, so an
/// endpoint or DTO cannot change without the frontend types changing with it.
///
/// To accept a change on purpose, regenerate the file and then the types:
///   PI_UPDATE_OPENAPI=1 dotnet test
///   cd src/web &amp;&amp; npm run generate:api
/// </summary>
public class OpenApiContractTests(AppFactory factory) : IClassFixture<AppFactory>
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    [Fact]
    public async Task The_server_matches_the_committed_api_contract()
    {
        var actual = await GetOpenApiDocumentAsync();
        var contractPath = Path.Combine(FindRepositoryRoot(), "src", "web", "src", "api", "openapi.json");

        if (Environment.GetEnvironmentVariable("PI_UPDATE_OPENAPI") == "1")
        {
            // Unix line endings whatever the machine: the file must be identical on Windows and in CI.
            var json = actual.ToJsonString(Indented).ReplaceLineEndings("\n") + "\n";
            await File.WriteAllTextAsync(contractPath, json, TestContext.Current.CancellationToken);
            return;
        }

        Assert.True(File.Exists(contractPath), $"Missing {contractPath}. Create it with PI_UPDATE_OPENAPI=1 dotnet test.");
        var expected = JsonNode.Parse(await File.ReadAllTextAsync(contractPath, TestContext.Current.CancellationToken));

        Assert.True(
            JsonNode.DeepEquals(expected, actual),
            "The API changed and src/web/src/api/openapi.json was not updated. If the change is intended, run " +
            "'PI_UPDATE_OPENAPI=1 dotnet test' and then 'npm run generate:api' in src/web, and commit both files.");
    }

    private async Task<JsonNode> GetOpenApiDocumentAsync()
    {
        // The document is off unless this setting turns it on, as in development.
        using var withOpenApi = factory.WithWebHostBuilder(builder => builder.UseSetting("OpenApi:Enabled", "true"));

        var json = await withOpenApi.CreateClient().GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        var document = JsonNode.Parse(json)!.AsObject();
        document.Remove("servers"); // the host it happens to run on is not part of the contract
        return document;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PresentacionesInteractivas.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not find the repository root.");
    }
}
