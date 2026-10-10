using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

namespace Server.Features.Import;

/// <summary>Reads what the importer needs straight from the PPTX with the Open XML SDK.</summary>
public static class PptxReader
{
    /// <summary>
    /// Speaker notes of every <b>visible</b> slide, in deck order ("" when a slide has none).
    /// Hidden slides are skipped because LibreOffice leaves them out of the PDF: including
    /// them would shift every later note onto the wrong image.
    /// </summary>
    /// <exception cref="InvalidDataException">The file is not a readable presentation.</exception>
    public static IReadOnlyList<string> ReadVisibleSlideNotes(string pptxPath)
    {
        try
        {
            using var document = PresentationDocument.Open(pptxPath, isEditable: false);
            var presentationPart = document.PresentationPart
                ?? throw new InvalidDataException("The file has no presentation part.");
            var slideIds = presentationPart.Presentation?.SlideIdList?.Elements<SlideId>() ?? [];

            var notes = new List<string>();
            foreach (var slideId in slideIds)
            {
                if (slideId.RelationshipId?.Value is not { } relationshipId
                    || presentationPart.GetPartById(relationshipId) is not SlidePart slidePart)
                {
                    throw new InvalidDataException("The presentation lists a slide that is missing from the file.");
                }

                var isHidden = slidePart.Slide?.Show?.Value == false;
                if (!isHidden)
                {
                    notes.Add(ReadNotes(slidePart));
                }
            }

            return notes;
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            // The SDK throws several unrelated exception types for corrupt packages.
            throw new InvalidDataException("The file is not a readable presentation.", ex);
        }
    }

    private static string ReadNotes(SlidePart slidePart)
    {
        var shapes = slidePart.NotesSlidePart?.NotesSlide?.CommonSlideData?.ShapeTree?.Elements<Shape>() ?? [];

        // The notes page also holds the slide thumbnail, the page number, the date...
        // Only the "body" placeholder is what the teacher typed.
        var body = shapes.FirstOrDefault(shape =>
            shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?
                .GetFirstChild<PlaceholderShape>()?.Type?.Value == PlaceholderValues.Body);

        var paragraphs = body?.TextBody?.Elements<A.Paragraph>() ?? [];
        var lines = paragraphs.Select(paragraph => string.Concat(paragraph.ChildElements.Select(TextOf)));
        return string.Join('\n', lines).Trim();
    }

    private static string TextOf(DocumentFormat.OpenXml.OpenXmlElement element) => element switch
    {
        A.Run run => run.Text?.Text ?? "",
        A.Field field => field.Text?.Text ?? "",
        A.Break => "\n",
        _ => "",
    };
}
