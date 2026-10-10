using System.Buffers.Binary;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Server.Domain;
using Server.Features.Join;
using Server.Features.Presentations;
using Server.Features.Sessions;
using Server.Realtime;
using Server.Tests.Sessions;

namespace Server.Tests.Import;

/// <summary>
/// Tests against the app running in its real container. It is the only test of the real
/// conversion: it uploads a deck (LibreOffice, pdftoppm and fonts included) and checks what
/// comes out. It is also the only one that runs a session over real WebSockets.
/// Runs when PI_SMOKE_URL and PI_SMOKE_KEY are set, as in the CI job that builds the image:
///   PI_SMOKE_URL=http://localhost:8080 PI_SMOKE_KEY=the-access-key
/// </summary>
public class ContainerSmokeTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_container_converts_a_deck_and_keeps_notes_aligned_past_a_hidden_slide()
    {
        var url = Environment.GetEnvironmentVariable("PI_SMOKE_URL");
        var key = Environment.GetEnvironmentVariable("PI_SMOKE_KEY");
        Assert.SkipWhen(url is null || key is null, "PI_SMOKE_URL and PI_SMOKE_KEY are not set.");

        using var client = new HttpClient { BaseAddress = new Uri(url!) };
        await SignInAsync(client, key!);

        var deck = SyntheticDeck.Create(
            new("Bienvenida", "Saludar a la clase.\nPreguntar: ¿qué es un ERP?"),
            new("Borrador oculto", "Esta nota no debe aparecer", Hidden: true),
            new("Evaluación", "Explicar los porcentajes"));
        var file = new ByteArrayContent(deck);
        file.Headers.ContentType = new MediaTypeHeaderValue(PptxUpload.ContentType);

        var upload = await client.PostAsync("/api/admin/presentations", new MultipartFormDataContent { { file, "file", "smoke.pptx" } }, Ct);
        upload.EnsureSuccessStatusCode();
        var summary = await upload.Content.ReadFromJsonAsync<PresentationSummary>(Json, Ct);

        var detail = await WaitForImportAsync(client, summary!.Id);
        Assert.Equal(ImportStatus.Ready, detail.Status);
        Assert.Equal(
            [
                new SlideDetail(0, "Saludar a la clase.\nPreguntar: ¿qué es un ERP?", 1),
                new SlideDetail(1, "Explicar los porcentajes", 1),
            ],
            detail.Slides);

        var first = await client.GetByteArrayAsync($"/api/admin/presentations/{summary.Id}/slides/0/steps/0", Ct);
        var second = await client.GetByteArrayAsync($"/api/admin/presentations/{summary.Id}/slides/1/steps/0", Ct);
        Assert.Equal((1600, 900), PngSize(first)); // 16:9 deck at the configured width
        Assert.Equal((1600, 900), PngSize(second));
        Assert.NotEqual(first, second);

        var pdf = await client.GetByteArrayAsync($"/api/admin/presentations/{summary.Id}/pdf", Ct);
        Assert.Equal("%PDF"u8.ToArray(), pdf[..4]);
    }

    /// <summary>
    /// The integration tests reach the hub through an in-memory server, which has no sockets.
    /// This is the one place where a session runs over real WebSockets, through Kestrel, with
    /// the cookies travelling on the WebSocket handshake as they do from a browser.
    /// </summary>
    [Fact]
    public async Task The_container_runs_a_live_session_over_websockets()
    {
        var url = Environment.GetEnvironmentVariable("PI_SMOKE_URL");
        var key = Environment.GetEnvironmentVariable("PI_SMOKE_KEY");
        Assert.SkipWhen(url is null || key is null, "PI_SMOKE_URL and PI_SMOKE_KEY are not set.");

        using var teacher = new HttpClient { BaseAddress = new Uri(url!) };
        var teacherCookie = await SignInAsync(teacher, key!);

        var file = new ByteArrayContent(SyntheticDeck.Create(new("Uno", "Primera"), new("Dos", "Segunda")));
        file.Headers.ContentType = new MediaTypeHeaderValue(PptxUpload.ContentType);
        var upload = await teacher.PostAsync("/api/admin/presentations", new MultipartFormDataContent { { file, "file", "live.pptx" } }, Ct);
        var presentation = await upload.Content.ReadFromJsonAsync<PresentationSummary>(Json, Ct);
        Assert.Equal(ImportStatus.Ready, (await WaitForImportAsync(teacher, presentation!.Id)).Status);

        var created = await teacher.PostAsJsonAsync("/api/admin/sessions", new CreateSessionRequest(presentation.Id), Ct);
        var session = (await created.Content.ReadFromJsonAsync<SessionInfo>(Json, Ct))!;

        using var student = new HttpClient { BaseAddress = new Uri(url!) };
        var joined = await student.PostAsJsonAsync($"/api/join/{session.Code}", new JoinRequest("Ana", "ana.smoke@example.com", Consent: true), Ct);
        joined.EnsureSuccessStatusCode();
        var studentCookie = joined.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
        student.DefaultRequestHeaders.Add("Cookie", studentCookie);

        await using var presenterConnection = WebSocketConnection(url!, session.Id, ClientRole.Presenter, teacherCookie);
        await using var studentConnection = WebSocketConnection(url!, session.Id, ClientRole.Participant, studentCookie);
        var presenterHub = new HubProbe(presenterConnection);
        var studentHub = new HubProbe(studentConnection);
        await presenterConnection.StartAsync(Ct);
        await presenterHub.ReadyOrClosedAsync(); // in its groups before the student arrives
        await studentConnection.StartAsync(Ct);

        Assert.Equal(session.Code, (await presenterHub.NextAsync<SessionSnapshot>()).Code);
        Assert.Equal(SessionStatus.Lobby, (await studentHub.NextAsync<SessionSnapshot>()).Status);
        var attendance = await presenterHub.NextAsync<AttendanceUpdate>(update => update.ConnectedCount == 1);
        Assert.Equal("Ana", Assert.Single(attendance.Attendees!).DisplayName);

        await presenterConnection.InvokeAsync(nameof(SessionHub.StartSession), Ct);
        await presenterConnection.InvokeAsync(nameof(SessionHub.GoTo), 1, 0, Ct);

        Assert.Equal(new HubProbe.SlideChanged(1, 0), await studentHub.NextAsync<HubProbe.SlideChanged>());
        var slide = await student.GetByteArrayAsync($"/api/sessions/{session.Id}/slides/1/steps/0", Ct);
        Assert.Equal((1600, 900), PngSize(slide));

        await presenterConnection.InvokeAsync(nameof(SessionHub.EndSession), Ct);
        await studentHub.NextAsync<HubProbe.Ended>();
    }

    private static HubConnection WebSocketConnection(string baseUrl, Guid sessionId, ClientRole role, string cookie) =>
        new HubConnectionBuilder()
            .WithUrl($"{baseUrl.TrimEnd('/')}{SessionHub.Path}?sessionId={sessionId}&role={role}", options =>
            {
                options.Transports = HttpTransportType.WebSockets; // no fallback: this must be a WebSocket
                options.Headers["Cookie"] = cookie;
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

    /// <summary>
    /// The teacher cookie is Secure and this talks plain http to localhost. A browser allows
    /// that; HttpClient's cookie container does not, so the cookie is passed by hand.
    /// </summary>
    private static async Task<string> SignInAsync(HttpClient client, string key)
    {
        var response = await client.PostAsJsonAsync("/api/access", new { key }, Ct);
        response.EnsureSuccessStatusCode();
        var cookie = response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return cookie;
    }

    private static async Task<PresentationDetail> WaitForImportAsync(HttpClient client, Guid id)
    {
        // The first conversion in a fresh container also creates the LibreOffice profile.
        for (var attempt = 0; attempt < 120; attempt++)
        {
            var detail = await client.GetFromJsonAsync<PresentationDetail>($"/api/admin/presentations/{id}", Json, Ct);
            if (detail!.Status is ImportStatus.Ready or ImportStatus.Failed)
            {
                return detail;
            }

            await Task.Delay(1000, Ct);
        }

        throw new TimeoutException("The import did not finish in 2 minutes.");
    }

    /// <summary>Width and height from the PNG header (IHDR chunk).</summary>
    private static (int Width, int Height) PngSize(byte[] png)
    {
        Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, png[..4]);
        return (BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)), BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));
    }
}
