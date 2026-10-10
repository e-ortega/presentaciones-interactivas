namespace Server.Domain;

public enum ImportStatus
{
    Pending,
    Processing,
    Ready,
    Failed,
}

/// <summary>A deck imported from a PPTX file. Slides are rendered as images (D-03).</summary>
public class Presentation
{
    private readonly List<Slide> _slides = [];

    private Presentation() { } // EF Core

    public Presentation(string title, string pptxPath, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        Title = title;
        PptxPath = pptxPath;
        Status = ImportStatus.Pending;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = "";
    public string PptxPath { get; private set; } = "";

    /// <summary>Student PDF, generated during import (D-14).</summary>
    public string? PdfPath { get; private set; }

    public ImportStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<Slide> Slides => _slides;
}
