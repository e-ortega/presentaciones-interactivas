using Server.Features.Import;

namespace Server.Tests.Import;

public sealed class PptxReaderTests : IDisposable
{
    private readonly List<string> _files = [];

    public void Dispose() => _files.ForEach(File.Delete);

    private string Deck(params SyntheticSlide[] slides)
    {
        var path = SyntheticDeck.CreateFile(slides);
        _files.Add(path);
        return path;
    }

    [Fact]
    public void Returns_the_notes_of_each_slide_in_deck_order()
    {
        var path = Deck(new("One", "First note"), new("Two", "Second note"), new("Three", "Third note"));

        Assert.Equal(["First note", "Second note", "Third note"], PptxReader.ReadVisibleSlideNotes(path));
    }

    [Fact]
    public void Skips_hidden_slides_so_notes_stay_aligned_with_the_images()
    {
        var path = Deck(
            new("One", "First note"),
            new("Hidden", "Note of the hidden slide", Hidden: true),
            new("Three", "Third note"));

        Assert.Equal(["First note", "Third note"], PptxReader.ReadVisibleSlideNotes(path));
    }

    [Fact]
    public void A_slide_without_notes_gets_an_empty_string()
    {
        var path = Deck(new("One", "First note"), new("No notes page"), new("Empty notes", ""), new("Four", "Fourth note"));

        Assert.Equal(["First note", "", "", "Fourth note"], PptxReader.ReadVisibleSlideNotes(path));
    }

    [Fact]
    public void Keeps_paragraphs_as_lines_and_leaves_out_the_slide_number()
    {
        var path = Deck(new SyntheticSlide("One", "Greet the class.\n\nAsk: what is an ERP?"));

        // The synthetic notes page also has a slide-number placeholder with the text "7".
        Assert.Equal(["Greet the class.\n\nAsk: what is an ERP?"], PptxReader.ReadVisibleSlideNotes(path));
    }

    [Fact]
    public void Keeps_characters_that_need_escaping_in_xml()
    {
        var path = Deck(new SyntheticSlide("One", "if (a < b && c > d) \"quote\" — ñ á"));

        Assert.Equal(["if (a < b && c > d) \"quote\" — ñ á"], PptxReader.ReadVisibleSlideNotes(path));
    }

    [Fact]
    public void Rejects_a_file_that_is_not_a_presentation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pi-bad-{Guid.NewGuid():N}.pptx");
        File.WriteAllText(path, "this is not a zip file");
        _files.Add(path);

        Assert.Throws<InvalidDataException>(() => PptxReader.ReadVisibleSlideNotes(path));
    }
}
