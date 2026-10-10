using System.Buffers.Binary;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Domain;
using Server.Features.Presentations;

namespace Server.Tests.Import;

/// <summary>
/// The only test of the real conversion: it uploads a deck to the app running in its
/// container (LibreOffice, pdftoppm and fonts included) and checks what comes out.
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
    /// The teacher cookie is Secure and this talks plain http to localhost. A browser allows
    /// that; HttpClient's cookie container does not, so the cookie is passed by hand.
    /// </summary>
    private static async Task SignInAsync(HttpClient client, string key)
    {
        var response = await client.PostAsJsonAsync("/api/access", new { key }, Ct);
        response.EnsureSuccessStatusCode();
        var cookie = response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
        client.DefaultRequestHeaders.Add("Cookie", cookie);
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
