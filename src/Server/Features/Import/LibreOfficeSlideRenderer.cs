using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using Server.Domain;

namespace Server.Features.Import;

public class ImportOptions
{
    public const string SectionName = "Import";

    public string SofficePath { get; set; } = "soffice";
    public string PdftoppmPath { get; set; } = "pdftoppm";

    /// <summary>LibreOffice is killed when a conversion takes longer than this.</summary>
    public TimeSpan ConversionTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Width of the slide images in pixels; the height follows the deck's aspect ratio.</summary>
    public int ImageWidth { get; set; } = 1600;

    public long MaxFileBytes { get; set; } = 100 * 1024 * 1024;
    public int MaxSlides { get; set; } = 300;
}

/// <summary>
/// v1 renderer: LibreOffice converts the PPTX to PDF, pdftoppm turns each page into a PNG and
/// the Open XML SDK reads the notes. One step per slide; animations are lost (D-25).
/// </summary>
public class LibreOfficeSlideRenderer(IOptions<ImportOptions> options, ILogger<LibreOfficeSlideRenderer> logger)
    : ISlideRenderer
{
    private readonly ImportOptions _options = options.Value;

    public async Task<RenderedPresentation> RenderAsync(string pptxPath, string workDirectory, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> notes;
        try
        {
            notes = PptxReader.ReadVisibleSlideNotes(pptxPath);
        }
        catch (InvalidDataException ex)
        {
            throw new SlideRenderException(ImportFailure.ConversionFailed, "The PPTX could not be read.", ex);
        }

        var pdfPath = await ConvertToPdfAsync(pptxPath, workDirectory, cancellationToken);
        var images = await RenderPagesAsync(pdfPath, workDirectory, cancellationToken);

        if (images.Count != notes.Count)
        {
            throw new SlideRenderException(
                ImportFailure.SlideCountMismatch,
                $"The PDF has {images.Count} pages but the deck has {notes.Count} visible slides.");
        }

        var slides = images.Select((image, index) => new RenderedSlide(notes[index], [image])).ToList();
        return new RenderedPresentation(pdfPath, slides);
    }

    private async Task<string> ConvertToPdfAsync(string pptxPath, string workDirectory, CancellationToken cancellationToken)
    {
        // A profile of its own per conversion: LibreOffice is not safe with a shared one.
        var profile = new Uri(Path.Combine(workDirectory, "lo-profile")).AbsoluteUri;
        var outputDirectory = Directory.CreateDirectory(Path.Combine(workDirectory, "pdf")).FullName;

        await RunAsync(
            _options.SofficePath,
            [$"-env:UserInstallation={profile}", "--headless", "--convert-to", "pdf", "--outdir", outputDirectory, pptxPath],
            cancellationToken);

        var pdfPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(pptxPath) + ".pdf");
        if (!File.Exists(pdfPath))
        {
            // soffice exits with 0 even when it cannot open the file.
            throw new SlideRenderException(ImportFailure.ConversionFailed, "LibreOffice did not produce a PDF.");
        }

        return pdfPath;
    }

    private async Task<IReadOnlyList<string>> RenderPagesAsync(string pdfPath, string workDirectory, CancellationToken cancellationToken)
    {
        var outputDirectory = Directory.CreateDirectory(Path.Combine(workDirectory, "pages")).FullName;

        await RunAsync(
            _options.PdftoppmPath,
            ["-png", "-scale-to-x", _options.ImageWidth.ToString(CultureInfo.InvariantCulture), "-scale-to-y", "-1", pdfPath, Path.Combine(outputDirectory, "page")],
            cancellationToken);

        // pdftoppm names the files page-1.png, page-01.png or page-001.png depending on the page count.
        return Directory.GetFiles(outputDirectory, "page-*.png")
            .OrderBy(path => int.Parse(Path.GetFileNameWithoutExtension(path)["page-".Length..], CultureInfo.InvariantCulture))
            .ToList();
    }

    private async Task RunAsync(string fileName, string[] arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.ConversionTimeout);

        Process process;
        try
        {
            process = Process.Start(startInfo)!;
        }
        catch (Exception ex)
        {
            throw new SlideRenderException(ImportFailure.Unexpected, $"Could not start '{fileName}'. Is it installed?", ex);
        }

        using (process)
        {
            // Read both streams while the process runs, or it blocks when a pipe fills up.
            var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            var error = process.StandardError.ReadToEndAsync(CancellationToken.None);

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                cancellationToken.ThrowIfCancellationRequested(); // the app is shutting down
                throw new SlideRenderException(
                    ImportFailure.ConversionTimedOut,
                    $"'{fileName}' took longer than {_options.ConversionTimeout.TotalSeconds:0} seconds.");
            }

            if (process.ExitCode != 0)
            {
                logger.LogWarning("{Program} exited with code {ExitCode}: {Error}", fileName, process.ExitCode, await error);
                throw new SlideRenderException(ImportFailure.ConversionFailed, $"'{fileName}' exited with code {process.ExitCode}.");
            }

            await output;
        }
    }
}
