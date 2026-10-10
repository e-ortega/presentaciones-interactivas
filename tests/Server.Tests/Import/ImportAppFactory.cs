using System.Data.Common;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Server.Domain;
using Server.Features.Import;
using Server.Infrastructure.Data;

namespace Server.Tests.Import;

/// <summary>
/// Tests that start the whole app, import worker included, against the shared test database.
/// They run one at a time, so a worker never picks up a presentation that belongs to another
/// test (whose files live in another storage folder).
/// </summary>
[CollectionDefinition(Name)]
public class ImportPipelineCollection
{
    public const string Name = "Import pipeline";
}

/// <summary>
/// The real app with a real database, queue and worker. Only LibreOffice is replaced, by
/// <see cref="FakeSlideRenderer"/>: the real conversion is covered by the container smoke test.
/// </summary>
public class ImportAppFactory(string connectionString) : WebApplicationFactory<Program>
{
    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "pi-tests-" + Guid.NewGuid().ToString("N"));

    public FakeSlideRenderer Renderer { get; } = new();

    public FlakyDatabase Database { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("AdminAccess:KeySha256", AppFactory.AccessKeyHash);
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Storage:LocalRoot", StorageRoot);
        builder.UseSetting("Import:MaxSlides", "5");
        builder.UseSetting("Import:RetryDelay", "00:00:00.050");

        builder.ConfigureTestServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<ISlideRenderer>();
            services.AddSingleton<ISlideRenderer>(Renderer);
            services.ConfigureDbContext<AppDbContext>(options => options.AddInterceptors(Database));
        });
    }

    /// <summary>Files stored for a presentation, as paths relative to its folder.</summary>
    public IReadOnlyList<string> StoredFiles(Guid presentationId)
    {
        var folder = Path.Combine(StorageRoot, "presentations", presentationId.ToString());
        return Directory.Exists(folder)
            ? [.. Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(folder, path).Replace('\\', '/'))
                .Order()]
            : [];
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(StorageRoot))
        {
            Directory.Delete(StorageRoot, recursive: true);
        }
    }
}

/// <summary>Reads the real notes and "renders" each visible slide as a placeholder file.</summary>
public class FakeSlideRenderer : ISlideRenderer
{
    public static readonly byte[] PdfBytes = "%PDF-fake"u8.ToArray();

    /// <summary>When set, every render fails with this reason.</summary>
    public ImportFailure? FailWith { get; set; }

    /// <summary>When set, the second slide points at an image that does not exist.</summary>
    public bool LoseSecondImage { get; set; }

    public static byte[] ImageBytes(int slideIndex) => System.Text.Encoding.UTF8.GetBytes($"image of slide {slideIndex}");

    public async Task<RenderedPresentation> RenderAsync(string pptxPath, string workDirectory, CancellationToken cancellationToken)
    {
        if (FailWith is { } failure)
        {
            throw new SlideRenderException(failure, "Simulated failure.");
        }

        var pdfPath = Path.Combine(workDirectory, "deck.pdf");
        await File.WriteAllBytesAsync(pdfPath, PdfBytes, cancellationToken);

        var slides = new List<RenderedSlide>();
        foreach (var notes in PptxReader.ReadVisibleSlideNotes(pptxPath))
        {
            var imagePath = Path.Combine(workDirectory, $"slide-{slides.Count}.png");
            if (!(LoseSecondImage && slides.Count == 1))
            {
                await File.WriteAllBytesAsync(imagePath, ImageBytes(slides.Count), cancellationToken);
            }

            slides.Add(new RenderedSlide(notes, [imagePath]));
        }

        return new RenderedPresentation(pdfPath, slides);
    }
}

/// <summary>
/// Makes the next database command whose SQL contains a given text fail once, the way a
/// paused or unreachable database would.
/// </summary>
public class FlakyDatabase : DbCommandInterceptor
{
    private string? _failCommandContaining;
    private TaskCompletionSource _failed = new();

    /// <summary>Completes when the armed failure has happened.</summary>
    public Task Failed => _failed.Task;

    public void FailNextCommandContaining(string sqlFragment)
    {
        _failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _failCommandContaining = sqlFragment;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        var fragment = _failCommandContaining;
        if (fragment is not null
            && command.CommandText.Contains(fragment, StringComparison.Ordinal)
            && Interlocked.CompareExchange(ref _failCommandContaining, null, fragment) == fragment)
        {
            _failed.SetResult();
            throw new InvalidOperationException("Simulated database outage.");
        }

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
