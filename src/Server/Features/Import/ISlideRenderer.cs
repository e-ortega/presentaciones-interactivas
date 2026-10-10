namespace Server.Features.Import;

/// <summary>
/// Turns a PPTX into slides with steps (images) and speaker notes (D-27).
/// The rest of the app only knows this interface, so the LibreOffice implementation can be
/// replaced by one that renders animations as several steps per slide.
/// </summary>
public interface ISlideRenderer
{
    /// <summary>Renders the deck into files inside <paramref name="workDirectory"/>.</summary>
    /// <exception cref="SlideRenderException">The deck could not be rendered.</exception>
    Task<RenderedPresentation> RenderAsync(string pptxPath, string workDirectory, CancellationToken cancellationToken);
}

/// <param name="PdfPath">Student PDF: the deck without notes (D-13).</param>
/// <param name="Slides">Visible slides, in deck order.</param>
public record RenderedPresentation(string PdfPath, IReadOnlyList<RenderedSlide> Slides);

/// <param name="StepImagePaths">One PNG per step, in build order. Never empty.</param>
public record RenderedSlide(string SpeakerNotes, IReadOnlyList<string> StepImagePaths);

public class SlideRenderException(Domain.ImportFailure failure, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public Domain.ImportFailure Failure { get; } = failure;
}
