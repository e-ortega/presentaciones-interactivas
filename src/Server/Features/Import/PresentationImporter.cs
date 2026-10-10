using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Server.Domain;
using Server.Infrastructure.Data;
using Server.Infrastructure.Storage;

namespace Server.Features.Import;

/// <summary>Imports one presentation: original PPTX in, slides, images and PDF out.</summary>
public class PresentationImporter(
    AppDbContext db,
    IFileStorage storage,
    ISlideRenderer renderer,
    ILogger<PresentationImporter> logger)
{
    public async Task ImportAsync(Guid presentationId, CancellationToken cancellationToken)
    {
        var presentation = await db.Presentations
            .Include(p => p.Slides).ThenInclude(s => s.Steps)
            .FirstOrDefaultAsync(p => p.Id == presentationId, cancellationToken);

        if (presentation is null || presentation.Status == ImportStatus.Ready)
        {
            return; // deleted, or already imported by an earlier run
        }

        presentation.StartProcessing();
        await db.SaveChangesAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        var workDirectory = Directory.CreateTempSubdirectory("pi-import-").FullName;
        try
        {
            var rendered = await RenderAsync(presentation, workDirectory, cancellationToken);
            await StoreAsync(presentation, rendered, cancellationToken);
            presentation.CompleteImport();
            logger.LogInformation(
                "Imported presentation {PresentationId}: {SlideCount} slides in {Seconds:0.0} s",
                presentation.Id, presentation.Slides.Count, stopwatch.Elapsed.TotalSeconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // shutting down: it stays in Processing and is picked up again at startup
        }
        catch (Exception ex)
        {
            var failure = ex is SlideRenderException renderException ? renderException.Failure : ImportFailure.Unexpected;
            logger.LogError(ex, "Import of presentation {PresentationId} failed: {Failure}", presentation.Id, failure);
            presentation.FailImport(failure);
        }
        finally
        {
            Directory.Delete(workDirectory, recursive: true);
        }

        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<RenderedPresentation> RenderAsync(Presentation presentation, string workDirectory, CancellationToken cancellationToken)
    {
        var pptxPath = Path.Combine(workDirectory, "deck.pptx");
        await using (var original = await storage.OpenReadAsync(presentation.PptxPath, cancellationToken)
            ?? throw new FileNotFoundException("The original PPTX is missing from storage.", presentation.PptxPath))
        await using (var local = File.Create(pptxPath))
        {
            await original.CopyToAsync(local, cancellationToken);
        }

        return await renderer.RenderAsync(pptxPath, workDirectory, cancellationToken);
    }

    private async Task StoreAsync(Presentation presentation, RenderedPresentation rendered, CancellationToken cancellationToken)
    {
        await UploadAsync(rendered.PdfPath, PresentationFiles.StudentPdf(presentation.Id), "application/pdf", cancellationToken);

        foreach (var renderedSlide in rendered.Slides)
        {
            var slide = presentation.AddSlide(renderedSlide.SpeakerNotes);
            foreach (var imagePath in renderedSlide.StepImagePaths)
            {
                var storagePath = PresentationFiles.StepImage(presentation.Id, slide.Index, slide.Steps.Count);
                await UploadAsync(imagePath, storagePath, "image/png", cancellationToken);
                slide.AddStep(storagePath);
            }
        }
    }

    private async Task UploadAsync(string localPath, string storagePath, string contentType, CancellationToken cancellationToken)
    {
        await using var file = File.OpenRead(localPath);
        await storage.SaveAsync(storagePath, file, contentType, cancellationToken);
    }
}
