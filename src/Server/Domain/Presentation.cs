namespace Server.Domain;

public enum ImportStatus
{
    Pending,
    Processing,
    Ready,
    Failed,
}

/// <summary>Why an import failed. The interface turns each value into a message for the teacher.</summary>
public enum ImportFailure
{
    /// <summary>LibreOffice could not convert the file.</summary>
    ConversionFailed,

    /// <summary>The conversion took longer than the allowed time.</summary>
    ConversionTimedOut,

    /// <summary>The images and the speaker notes do not line up slide by slide.</summary>
    SlideCountMismatch,

    Unexpected,
}

/// <summary>A deck imported from a PPTX file. Slides are rendered as images (D-03).</summary>
public class Presentation
{
    private readonly List<Slide> _slides = [];

    private Presentation() { } // EF Core

    public Presentation(string title, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        Title = title;
        PptxPath = PresentationFiles.Original(Id);
        Status = ImportStatus.Pending;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = "";
    public string PptxPath { get; private set; } = "";

    /// <summary>Student PDF, generated during import (D-14).</summary>
    public string? PdfPath { get; private set; }

    public ImportStatus Status { get; private set; }
    public ImportFailure? Failure { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<Slide> Slides => _slides;

    /// <summary>
    /// Also valid from Processing or Failed: an import interrupted by a restart starts over.
    /// </summary>
    public void StartProcessing()
    {
        if (Status == ImportStatus.Ready)
        {
            throw new InvalidOperationException("A presentation that is ready cannot be imported again.");
        }

        Status = ImportStatus.Processing;
        Failure = null;
    }

    /// <summary>Adds the next slide, in deck order.</summary>
    public Slide AddSlide(string speakerNotes)
    {
        EnsureProcessing();
        var slide = new Slide(Id, _slides.Count, speakerNotes);
        _slides.Add(slide);
        return slide;
    }

    public void CompleteImport()
    {
        EnsureProcessing();
        if (_slides.Count == 0 || _slides.Any(slide => slide.Steps.Count == 0))
        {
            throw new InvalidOperationException("Every presentation needs slides, and every slide at least one step.");
        }

        PdfPath = PresentationFiles.StudentPdf(Id);
        Status = ImportStatus.Ready;
    }

    public void FailImport(ImportFailure failure)
    {
        EnsureProcessing();
        _slides.Clear();
        Status = ImportStatus.Failed;
        Failure = failure;
    }

    private void EnsureProcessing()
    {
        if (Status != ImportStatus.Processing)
        {
            throw new InvalidOperationException($"The presentation is {Status}, not being processed.");
        }
    }
}

/// <summary>Where the files of a presentation live in <c>IFileStorage</c>.</summary>
public static class PresentationFiles
{
    public static string Original(Guid presentationId) => $"presentations/{presentationId}/original.pptx";

    public static string StudentPdf(Guid presentationId) => $"presentations/{presentationId}/student.pdf";

    public static string StepImage(Guid presentationId, int slideIndex, int stepIndex) =>
        $"presentations/{presentationId}/slides/{slideIndex}/steps/{stepIndex}.png";
}
