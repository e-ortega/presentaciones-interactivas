using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Domain;
using Server.Features.Presentations;
using Server.Tests.Data;

namespace Server.Tests.Import;

/// <summary>What happens to an import when things around it go wrong.</summary>
[Collection(ImportPipelineCollection.Name)]
public sealed class ImportResilienceTests(SqlServerFixture sql) : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private ImportAppFactory? _factory;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _factory?.Dispose();

    private async Task<ImportAppFactory> CreateFactoryAsync() =>
        _factory = new ImportAppFactory(await sql.GetConnectionStringAsync());

    private static MultipartFormDataContent Form(byte[] content, string fileName)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(PptxUpload.ContentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private static byte[] TwoSlideDeck() => SyntheticDeck.Create(new("One", "First"), new("Two", "Second"));

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
    public async Task An_import_that_could_not_reach_the_database_is_tried_again()
    {
        var factory = await CreateFactoryAsync();
        var client = await AppFactory.SignInAsync(factory);
        // The importer's first query is the only one that loads a presentation with its steps.
        factory.Database.FailNextCommandContaining("[SlideSteps]");

        var upload = await client.PostAsync("/api/admin/presentations", Form(TwoSlideDeck(), "deck.pptx"), Ct);
        var summary = await upload.Content.ReadFromJsonAsync<PresentationSummary>(Json, Ct);
        await factory.Database.Failed.WaitAsync(TimeSpan.FromSeconds(10), Ct);

        var detail = await WaitForImportAsync(client, summary!.Id);
        Assert.Equal(ImportStatus.Ready, detail.Status);
        Assert.Equal(2, detail.Slides.Count);
    }

    [Fact]
    public async Task A_failed_import_leaves_only_the_original_file_in_storage()
    {
        var factory = await CreateFactoryAsync();
        factory.Renderer.LoseSecondImage = true; // fails after the PDF and the first image were stored
        var client = await AppFactory.SignInAsync(factory);

        var upload = await client.PostAsync("/api/admin/presentations", Form(TwoSlideDeck(), "deck.pptx"), Ct);
        var summary = await upload.Content.ReadFromJsonAsync<PresentationSummary>(Json, Ct);
        var detail = await WaitForImportAsync(client, summary!.Id);

        Assert.Equal(ImportStatus.Failed, detail.Status);
        Assert.Equal(ImportFailure.Unexpected, detail.Failure);
        Assert.Equal(["original.pptx"], factory.StoredFiles(summary.Id));
    }

    [Fact]
    public async Task An_upload_that_cannot_be_saved_leaves_no_file_behind()
    {
        var factory = await CreateFactoryAsync();
        var client = await AppFactory.SignInAsync(factory);
        factory.Database.FailNextCommandContaining("INSERT INTO [Presentations]");

        var upload = await client.PostAsync("/api/admin/presentations", Form(TwoSlideDeck(), "deck.pptx"), Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, upload.StatusCode);
        var presentations = Path.Combine(factory.StorageRoot, "presentations");
        Assert.False(Directory.Exists(presentations) && Directory.EnumerateFiles(presentations, "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task Only_an_image_that_exists_is_cached_by_the_browser()
    {
        var client = await AppFactory.SignInAsync(await CreateFactoryAsync());
        var upload = await client.PostAsync("/api/admin/presentations", Form(TwoSlideDeck(), "deck.pptx"), Ct);
        var summary = await upload.Content.ReadFromJsonAsync<PresentationSummary>(Json, Ct);
        await WaitForImportAsync(client, summary!.Id);

        var found = await client.GetAsync($"/api/admin/presentations/{summary.Id}/slides/0/steps/0", Ct);
        var missing = await client.GetAsync($"/api/admin/presentations/{summary.Id}/slides/9/steps/0", Ct);

        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.True(found.Headers.CacheControl is { Private: true, MaxAge.TotalHours: 24 });
        Assert.Contains("immutable", found.Headers.CacheControl.ToString(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Null(missing.Headers.CacheControl);
    }

    [Fact]
    public async Task A_deck_over_the_slide_limit_is_rejected_with_that_limit()
    {
        var client = await AppFactory.SignInAsync(await CreateFactoryAsync());
        var tooMany = SyntheticDeck.Create([.. Enumerable.Range(1, 6).Select(n => new SyntheticSlide($"Slide {n}"))]);

        var upload = await client.PostAsync("/api/admin/presentations", Form(tooMany, "big.pptx"), Ct);

        var problem = await upload.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("TooManySlides", problem.GetProperty("code").GetString());
        Assert.Equal(5, problem.GetProperty("limit").GetInt32()); // Import:MaxSlides in this factory
    }
}
