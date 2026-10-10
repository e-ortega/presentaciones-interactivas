using Server.Domain;

namespace Server.Tests.Domain;

public class PresentationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 15, 0, 0, TimeSpan.Zero);

    private static Presentation Processing()
    {
        var presentation = new Presentation("Deck", Now);
        presentation.StartProcessing();
        return presentation;
    }

    [Fact]
    public void A_new_presentation_is_pending_and_knows_where_its_original_file_goes()
    {
        var presentation = new Presentation("Deck", Now);

        Assert.Equal(ImportStatus.Pending, presentation.Status);
        Assert.Equal($"presentations/{presentation.Id}/original.pptx", presentation.PptxPath);
    }

    [Fact]
    public void Slides_and_steps_are_numbered_in_the_order_they_are_added()
    {
        var presentation = Processing();

        var first = presentation.AddSlide("one");
        var second = presentation.AddSlide("two");
        second.AddStep("a.png");
        var secondStep = second.AddStep("b.png");

        Assert.Equal((0, 1), (first.Index, second.Index));
        Assert.Equal(1, secondStep.Index);
    }

    [Fact]
    public void Completing_the_import_makes_it_ready_with_its_pdf()
    {
        var presentation = Processing();
        presentation.AddSlide("notes").AddStep("slide.png");

        presentation.CompleteImport();

        Assert.Equal(ImportStatus.Ready, presentation.Status);
        Assert.Equal($"presentations/{presentation.Id}/student.pdf", presentation.PdfPath);
    }

    [Fact]
    public void An_import_cannot_complete_without_slides_or_with_a_slide_without_steps()
    {
        var empty = Processing();
        var slideWithoutSteps = Processing();
        slideWithoutSteps.AddSlide("notes");

        Assert.Throws<InvalidOperationException>(empty.CompleteImport);
        Assert.Throws<InvalidOperationException>(slideWithoutSteps.CompleteImport);
    }

    [Fact]
    public void A_failed_import_keeps_the_reason_and_drops_partial_slides()
    {
        var presentation = Processing();
        presentation.AddSlide("notes").AddStep("slide.png");

        presentation.FailImport(ImportFailure.ConversionFailed);

        Assert.Equal(ImportStatus.Failed, presentation.Status);
        Assert.Equal(ImportFailure.ConversionFailed, presentation.Failure);
        Assert.Empty(presentation.Slides);
    }

    [Fact]
    public void A_failed_import_can_be_tried_again_but_a_ready_one_cannot()
    {
        var failed = Processing();
        failed.FailImport(ImportFailure.Unexpected);
        var ready = Processing();
        ready.AddSlide("notes").AddStep("slide.png");
        ready.CompleteImport();

        failed.StartProcessing();

        Assert.Equal(ImportStatus.Processing, failed.Status);
        Assert.Null(failed.Failure);
        Assert.Throws<InvalidOperationException>(ready.StartProcessing);
    }

    [Fact]
    public void Slides_cannot_be_added_outside_an_import()
    {
        var pending = new Presentation("Deck", Now);

        Assert.Throws<InvalidOperationException>(() => pending.AddSlide("notes"));
    }
}
