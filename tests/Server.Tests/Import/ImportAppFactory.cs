using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Server.Domain;
using Server.Features.Import;

namespace Server.Tests.Import;

/// <summary>
/// The real app with a real database, queue and worker. Only LibreOffice is replaced, by
/// <see cref="FakeSlideRenderer"/>: the real conversion is covered by the container smoke test.
/// </summary>
public class ImportAppFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "pi-tests-" + Guid.NewGuid().ToString("N"));

    public FakeSlideRenderer Renderer { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("AdminAccess:KeySha256", AppFactory.AccessKeyHash);
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Storage:LocalRoot", _storageRoot);
        builder.UseSetting("Import:MaxSlides", "5");

        builder.ConfigureTestServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<ISlideRenderer>();
            services.AddSingleton<ISlideRenderer>(Renderer);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }
}

/// <summary>Reads the real notes and "renders" each visible slide as a placeholder file.</summary>
public class FakeSlideRenderer : ISlideRenderer
{
    public static readonly byte[] PdfBytes = "%PDF-fake"u8.ToArray();

    /// <summary>When set, every render fails with this reason.</summary>
    public ImportFailure? FailWith { get; set; }

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
            await File.WriteAllBytesAsync(imagePath, ImageBytes(slides.Count), cancellationToken);
            slides.Add(new RenderedSlide(notes, [imagePath]));
        }

        return new RenderedPresentation(pdfPath, slides);
    }
}
