namespace Server.Domain;

public class Slide
{
    private readonly List<SlideStep> _steps = [];
    private readonly List<Widget> _widgets = [];

    private Slide() { } // EF Core

    public Slide(Guid presentationId, int index, string speakerNotes)
    {
        Id = Guid.CreateVersion7();
        PresentationId = presentationId;
        Index = index;
        SpeakerNotes = speakerNotes;
    }

    public Guid Id { get; private set; }
    public Guid PresentationId { get; private set; }

    /// <summary>Zero-based position among the visible slides of the deck.</summary>
    public int Index { get; private set; }

    public string SpeakerNotes { get; private set; } = "";

    public IReadOnlyList<SlideStep> Steps => _steps;
    public IReadOnlyList<Widget> Widgets => _widgets;

    /// <summary>Adds the next step, in build order.</summary>
    public SlideStep AddStep(string imagePath)
    {
        var step = new SlideStep(Id, _steps.Count, imagePath);
        _steps.Add(step);
        return step;
    }
}

/// <summary>
/// One build state of a slide. Navigation is always by (slide, step) so animations can be
/// added later without touching sync (D-26). In v1 every slide has exactly one step.
/// </summary>
public class SlideStep
{
    private SlideStep() { } // EF Core

    public SlideStep(Guid slideId, int index, string imagePath)
    {
        Id = Guid.CreateVersion7();
        SlideId = slideId;
        Index = index;
        ImagePath = imagePath;
    }

    public Guid Id { get; private set; }
    public Guid SlideId { get; private set; }
    public int Index { get; private set; }
    public string ImagePath { get; private set; } = "";
}
