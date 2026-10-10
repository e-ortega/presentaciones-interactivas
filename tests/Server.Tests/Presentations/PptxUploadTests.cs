using Server.Features.Import;
using Server.Features.Presentations;
using Server.Tests.Import;

namespace Server.Tests.Presentations;

public class PptxUploadTests
{
    private static readonly ImportOptions Options = new() { MaxFileBytes = 1024 * 1024, MaxSlides = 3 };

    private static MemoryStream Deck(int slides) =>
        new(SyntheticDeck.Create([.. Enumerable.Range(1, slides).Select(n => new SyntheticSlide($"Slide {n}"))]));

    [Theory]
    [InlineData("Semana 01.pptx", "Semana 01")]
    [InlineData("  Semana 01.PPTX ", "Semana 01")]
    [InlineData(@"C:\decks\Semana 01.pptx", "Semana 01")] // a Windows path, also when the server runs on Linux
    [InlineData("/home/ana/decks/Semana 01.pptx", "Semana 01")]
    [InlineData("v1.2 final.pptx", "v1.2 final")]
    [InlineData(".pptx", ".pptx")] // never an empty title
    public void The_title_is_the_file_name_without_folders_or_extension(string fileName, string expected)
    {
        Assert.Equal(expected, PptxUpload.TitleFrom(fileName));
    }

    [Fact]
    public void A_very_long_file_name_is_cut_to_the_title_length()
    {
        Assert.Equal(200, PptxUpload.TitleFrom(new string('a', 300) + ".pptx").Length);
    }

    [Fact]
    public void A_valid_deck_passes_and_its_stream_can_still_be_read()
    {
        using var deck = Deck(slides: 3);

        Assert.Null(PptxUpload.Validate("deck.pptx", deck, Options));

        deck.Position = 0;
        Assert.Equal(0x50, deck.ReadByte()); // still open: the upload stores it next
    }

    [Theory]
    [InlineData("deck.pptm")]
    [InlineData("deck.ppt")]
    [InlineData("deck")]
    public void Anything_not_named_pptx_is_rejected(string fileName)
    {
        using var deck = Deck(slides: 1);

        Assert.Equal(UploadError.NotPptx, PptxUpload.Validate(fileName, deck, Options));
    }

    [Fact]
    public void A_file_over_the_size_limit_is_rejected()
    {
        using var large = new MemoryStream(new byte[Options.MaxFileBytes + 1]);

        Assert.Equal(UploadError.TooLarge, PptxUpload.Validate("deck.pptx", large, Options));
    }

    [Fact]
    public void A_file_that_is_not_a_zip_or_not_a_presentation_is_unreadable()
    {
        using var text = new MemoryStream("not a zip"u8.ToArray());
        using var emptyZip = new MemoryStream([0x50, 0x4B, 0x03, 0x04, 0, 0, 0, 0]);

        Assert.Equal(UploadError.Unreadable, PptxUpload.Validate("deck.pptx", text, Options));
        Assert.Equal(UploadError.Unreadable, PptxUpload.Validate("deck.pptx", emptyZip, Options));
    }

    [Fact]
    public void A_deck_over_the_slide_limit_is_rejected()
    {
        using var deck = Deck(slides: 4);

        Assert.Equal(UploadError.TooManySlides, PptxUpload.Validate("deck.pptx", deck, Options));
    }
}
