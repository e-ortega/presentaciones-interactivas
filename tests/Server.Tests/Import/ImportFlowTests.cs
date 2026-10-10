using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Domain;
using Server.Features.Presentations;
using Server.Tests.Data;

namespace Server.Tests.Import;

/// <summary>Upload, background import and reading the result, through the HTTP API.</summary>
[Collection(ImportPipelineCollection.Name)]
public sealed class ImportFlowTests(SqlServerFixture sql) : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private ImportAppFactory? _factory;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _factory?.Dispose();

    /// <summary>Created inside each test so a missing SQL Server skips the test.</summary>
    private async Task<ImportAppFactory> CreateFactoryAsync() =>
        _factory = new ImportAppFactory(await sql.GetConnectionStringAsync());

    private static MultipartFormDataContent Form(byte[] content, string fileName)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(PptxUpload.ContentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private static async Task<PresentationDetail> WaitForImportAsync(HttpClient client, Guid id)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var detail = await client.GetFromJsonAsync<PresentationDetail>($"/api/admin/presentations/{id}", Json, Ct);
            if (detail!.Status is ImportStatus.Ready or ImportStatus.Failed)
            {
                return detail;
            }

            await Task.Delay(100, Ct);
        }

        throw new TimeoutException("The import did not finish in 10 seconds.");
    }

    [Fact]
    public async Task An_uploaded_deck_becomes_slides_with_notes_images_and_a_pdf()
    {
        var client = await AppFactory.SignInAsync(await CreateFactoryAsync());
        var deck = SyntheticDeck.Create(
            new("Intro", "Welcome everyone"),
            new("Draft", "Not for today", Hidden: true),
            new("ERP", "Ask what an ERP is"));

        var upload = await client.PostAsync("/api/admin/presentations", Form(deck, "Semana 01.pptx"), Ct);

        Assert.Equal(HttpStatusCode.Accepted, upload.StatusCode);
        var summary = await upload.Content.ReadFromJsonAsync<PresentationSummary>(Json, Ct);
        Assert.Equal("Semana 01", summary!.Title);
        Assert.Equal(ImportStatus.Pending, summary.Status);

        var detail = await WaitForImportAsync(client, summary.Id);
        Assert.Equal(ImportStatus.Ready, detail.Status);
        Assert.Equal(
            [new SlideDetail(0, "Welcome everyone", 1), new SlideDetail(1, "Ask what an ERP is", 1)],
            detail.Slides);

        var secondImage = await client.GetAsync($"/api/admin/presentations/{summary.Id}/slides/1/steps/0", Ct);
        Assert.Equal("image/png", secondImage.Content.Headers.ContentType?.MediaType);
        Assert.Equal(FakeSlideRenderer.ImageBytes(1), await secondImage.Content.ReadAsByteArrayAsync(Ct));

        var pdf = await client.GetAsync($"/api/admin/presentations/{summary.Id}/pdf", Ct);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.Equal(FakeSlideRenderer.PdfBytes, await pdf.Content.ReadAsByteArrayAsync(Ct));

        var list = await client.GetFromJsonAsync<List<PresentationSummary>>("/api/admin/presentations", Json, Ct);
        Assert.Contains(list!, p => p.Id == summary.Id && p.SlideCount == 2 && p.Status == ImportStatus.Ready);
    }

    [Fact]
    public async Task A_failed_conversion_is_reported_with_its_reason_and_leaves_no_slides()
    {
        var factory = await CreateFactoryAsync();
        factory.Renderer.FailWith = ImportFailure.ConversionTimedOut;
        var client = await AppFactory.SignInAsync(factory);

        var upload = await client.PostAsync("/api/admin/presentations", Form(SyntheticDeck.Create(new SyntheticSlide("One")), "deck.pptx"), Ct);
        var summary = await upload.Content.ReadFromJsonAsync<PresentationSummary>(Json, Ct);
        var detail = await WaitForImportAsync(client, summary!.Id);

        Assert.Equal(ImportStatus.Failed, detail.Status);
        Assert.Equal(ImportFailure.ConversionTimedOut, detail.Failure);
        Assert.Empty(detail.Slides);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/admin/presentations/{summary.Id}/pdf", Ct)).StatusCode);
    }

    [Theory]
    [InlineData("deck.pptm", "NotPptx")]
    [InlineData("deck.ppt", "NotPptx")]
    [InlineData("deck.pptx", "Unreadable")]
    public async Task Files_that_are_not_a_pptx_are_rejected_with_a_code(string fileName, string expectedCode)
    {
        var client = await AppFactory.SignInAsync(await CreateFactoryAsync());

        var upload = await client.PostAsync("/api/admin/presentations", Form("not a presentation"u8.ToArray(), fileName), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        var problem = await upload.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Decks_over_the_slide_limit_or_with_every_slide_hidden_are_rejected()
    {
        var client = await AppFactory.SignInAsync(await CreateFactoryAsync());
        var tooMany = SyntheticDeck.Create([.. Enumerable.Range(1, 6).Select(n => new SyntheticSlide($"Slide {n}"))]); // limit is 5 here
        var allHidden = SyntheticDeck.Create(new SyntheticSlide("Hidden", Hidden: true));

        var tooManyProblem = await (await client.PostAsync("/api/admin/presentations", Form(tooMany, "big.pptx"), Ct))
            .Content.ReadFromJsonAsync<JsonElement>(Ct);
        var allHiddenProblem = await (await client.PostAsync("/api/admin/presentations", Form(allHidden, "hidden.pptx"), Ct))
            .Content.ReadFromJsonAsync<JsonElement>(Ct);

        Assert.Equal("TooManySlides", tooManyProblem.GetProperty("code").GetString());
        Assert.Equal("NoVisibleSlides", allHiddenProblem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Presentations_are_only_for_the_teacher()
    {
        var anonymous = (await CreateFactoryAsync()).CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/admin/presentations", Ct)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.PostAsync("/api/admin/presentations", Form(SyntheticDeck.Create(new SyntheticSlide("One")), "deck.pptx"), Ct)).StatusCode);
    }

    [Fact]
    public async Task Unknown_presentation_or_slide_is_a_404()
    {
        var client = await AppFactory.SignInAsync(await CreateFactoryAsync());
        var missing = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/admin/presentations/{missing}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/admin/presentations/{missing}/slides/0/steps/0", Ct)).StatusCode);
    }
}
