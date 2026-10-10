using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Server.Domain;
using Server.Features.Import;
using Server.Infrastructure.Data;
using Server.Infrastructure.Storage;

namespace Server.Features.Presentations;

public record PresentationSummary(
    Guid Id,
    string Title,
    ImportStatus Status,
    ImportFailure? Failure,
    int SlideCount,
    DateTimeOffset CreatedAt);

public record PresentationDetail(
    Guid Id,
    string Title,
    ImportStatus Status,
    ImportFailure? Failure,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SlideDetail> Slides);

public record SlideDetail(int Index, string SpeakerNotes, int StepCount);

public static class PresentationEndpoints
{
    public static void MapPresentationEndpoints(this RouteGroupBuilder admin, ImportOptions importOptions)
    {
        var group = admin.MapGroup("/presentations");

        group.MapPost("/", Upload)
            // Room for the multipart envelope on top of the file itself.
            .WithMetadata(new RequestSizeLimitAttribute(importOptions.MaxFileBytes + 1024 * 1024))
            // The teacher cookie is SameSite=Lax, so another site cannot make this POST.
            .DisableAntiforgery();

        group.MapGet("/", List);
        group.MapGet("/{id:guid}", Get);
        group.MapGet("/{id:guid}/pdf", GetPdf);
        group.MapGet("/{id:guid}/slides/{slideIndex:int}/steps/{stepIndex:int}", GetStepImage);
    }

    private static async Task<IResult> Upload(
        IFormFile? file,
        AppDbContext db,
        IFileStorage storage,
        ImportQueue queue,
        IOptions<ImportOptions> options,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return UploadProblem(UploadError.NoFile);
        }

        // Checked on a local copy: the Open XML SDK needs a seekable file.
        var tempPath = Path.GetTempFileName();
        try
        {
            await using (var temp = File.Create(tempPath))
            {
                await file.CopyToAsync(temp, cancellationToken);
            }

            if (PptxUpload.Validate(file.FileName, tempPath, options.Value) is { } error)
            {
                return UploadProblem(error);
            }

            var presentation = new Presentation(PptxUpload.TitleFrom(file.FileName), time.GetUtcNow());
            await using (var temp = File.OpenRead(tempPath))
            {
                await storage.SaveAsync(presentation.PptxPath, temp, PptxUpload.ContentType, cancellationToken);
            }

            db.Presentations.Add(presentation);
            await db.SaveChangesAsync(cancellationToken);
            queue.Enqueue(presentation.Id);

            return Results.Accepted($"/api/admin/presentations/{presentation.Id}", ToSummary(presentation, slideCount: 0));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static async Task<IReadOnlyList<PresentationSummary>> List(AppDbContext db, CancellationToken cancellationToken) =>
        await db.Presentations
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PresentationSummary(p.Id, p.Title, p.Status, p.Failure, p.Slides.Count, p.CreatedAt))
            .ToListAsync(cancellationToken);

    private static async Task<IResult> Get(Guid id, AppDbContext db, CancellationToken cancellationToken)
    {
        var presentation = await db.Presentations
            .Where(p => p.Id == id)
            .Select(p => new PresentationDetail(
                p.Id,
                p.Title,
                p.Status,
                p.Failure,
                p.CreatedAt,
                p.Slides.OrderBy(s => s.Index).Select(s => new SlideDetail(s.Index, s.SpeakerNotes, s.Steps.Count)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return presentation is null ? Results.Problem(statusCode: StatusCodes.Status404NotFound) : Results.Ok(presentation);
    }

    private static async Task<IResult> GetPdf(Guid id, AppDbContext db, IFileStorage storage, CancellationToken cancellationToken)
    {
        var pdfPath = await db.Presentations
            .Where(p => p.Id == id)
            .Select(p => p.PdfPath)
            .FirstOrDefaultAsync(cancellationToken);

        return await FileOrNotFound(storage, pdfPath, "application/pdf", cancellationToken);
    }

    private static async Task<IResult> GetStepImage(
        Guid id, int slideIndex, int stepIndex, AppDbContext db, IFileStorage storage, HttpContext http, CancellationToken cancellationToken)
    {
        var imagePath = await db.Slides
            .Where(s => s.PresentationId == id && s.Index == slideIndex)
            .SelectMany(s => s.Steps)
            .Where(step => step.Index == stepIndex)
            .Select(step => step.ImagePath)
            .FirstOrDefaultAsync(cancellationToken);

        // The image of a step never changes once imported. "private": only this browser keeps it.
        http.Response.Headers.CacheControl = "private, max-age=86400, immutable";
        return await FileOrNotFound(storage, imagePath, "image/png", cancellationToken);
    }

    private static async Task<IResult> FileOrNotFound(IFileStorage storage, string? path, string contentType, CancellationToken cancellationToken)
    {
        var stream = path is null ? null : await storage.OpenReadAsync(path, cancellationToken);
        return stream is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound)
            : Results.Stream(stream, contentType);
    }

    private static PresentationSummary ToSummary(Presentation p, int slideCount) =>
        new(p.Id, p.Title, p.Status, p.Failure, slideCount, p.CreatedAt);

    /// <summary>400 with a machine-readable "code" the interface turns into a message.</summary>
    private static IResult UploadProblem(UploadError error) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "The file cannot be imported.",
            extensions: new Dictionary<string, object?> { ["code"] = error.ToString() });
}
