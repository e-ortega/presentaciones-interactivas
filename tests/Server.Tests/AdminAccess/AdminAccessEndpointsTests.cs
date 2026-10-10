using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace Server.Tests.AdminAccess;

public class AdminAccessEndpointsTests(AppFactory factory) : IClassFixture<AppFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_endpoint_answers_without_a_database()
    {
        var response = await factory.CreateHttpsClient().GetAsync("/health", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_area_rejects_requests_without_the_cookie()
    {
        var response = await factory.CreateHttpsClient().GetAsync("/api/admin/access", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_key_is_rejected_and_sets_no_cookie()
    {
        var client = factory.CreateHttpsClient();

        var signIn = await client.PostAsJsonAsync("/api/access", new { key = "wrong" }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, signIn.StatusCode);
        Assert.False(signIn.Headers.Contains("Set-Cookie"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/access", Ct)).StatusCode);
    }

    [Fact]
    public async Task Right_key_opens_the_teacher_area_until_sign_out()
    {
        var client = factory.CreateHttpsClient();

        var signIn = await client.PostAsJsonAsync("/api/access", new { key = AppFactory.AccessKey }, Ct);
        Assert.Equal(HttpStatusCode.NoContent, signIn.StatusCode);

        var cookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/admin/access", Ct)).StatusCode);

        await client.DeleteAsync("/api/admin/access", Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/access", Ct)).StatusCode);
    }

    [Fact]
    public async Task Without_a_configured_hash_the_answer_is_not_configured_instead_of_wrong_key()
    {
        using var unconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting("AdminAccess:KeySha256", ""));
        var client = unconfigured.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        var signIn = await client.PostAsJsonAsync("/api/access", new { key = AppFactory.AccessKey }, Ct);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, signIn.StatusCode);
        var problem = await signIn.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(Ct);
        Assert.Equal("AccessKeyNotConfigured", problem.GetProperty("code").GetString());
        Assert.False(signIn.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Unknown_api_route_is_a_404_not_the_react_page()
    {
        var response = await factory.CreateHttpsClient().GetAsync("/api/does-not-exist", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
