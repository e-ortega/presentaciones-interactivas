using System.Security;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

namespace Server.Tests.Import;

/// <param name="Notes">Speaker notes; null for a slide without a notes page.</param>
public record SyntheticSlide(string Title, string? Notes = null, bool Hidden = false);

/// <summary>
/// Builds small, valid PPTX files for tests. The repository is public, so tests never use
/// real decks: every case (hidden slides, missing notes...) is created here.
/// </summary>
public static class SyntheticDeck
{
    private const string Namespaces =
        "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" " +
        "xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"";

    private const string EmptyShapeTree =
        "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr/>";

    public static byte[] Create(params SyntheticSlide[] slides)
    {
        using var stream = new MemoryStream();
        using (var document = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
        {
            var presentation = document.AddPresentationPart();
            var master = presentation.AddNewPart<SlideMasterPart>("rIdMaster");
            var layout = master.AddNewPart<SlideLayoutPart>("rIdLayout");
            var theme = master.AddNewPart<ThemePart>("rIdTheme");
            layout.AddPart(master, "rIdMaster");

            Write(theme, ThemeXml);
            Write(master, MasterXml);
            Write(layout, LayoutXml);

            var slideIds = new StringBuilder();
            for (var i = 0; i < slides.Length; i++)
            {
                var slidePart = presentation.AddNewPart<SlidePart>($"rIdSlide{i}");
                slidePart.AddPart(layout, "rIdLayout");
                Write(slidePart, SlideXml(slides[i]));

                if (slides[i].Notes is { } notes)
                {
                    var notesPart = slidePart.AddNewPart<NotesSlidePart>("rIdNotes");
                    notesPart.AddPart(slidePart, "rIdSlide");
                    Write(notesPart, NotesXml(notes));
                }

                slideIds.Append($"<p:sldId id=\"{256 + i}\" r:id=\"rIdSlide{i}\"/>");
            }

            Write(presentation, PresentationXml(slideIds.ToString()));
        }

        return stream.ToArray();
    }

    /// <summary>Writes the deck to a temporary .pptx file and returns its path.</summary>
    public static string CreateFile(params SyntheticSlide[] slides)
    {
        var path = Path.Combine(Path.GetTempPath(), $"pi-deck-{Guid.NewGuid():N}.pptx");
        File.WriteAllBytes(path, Create(slides));
        return path;
    }

    private static void Write(OpenXmlPart part, string xml)
    {
        using var stream = part.GetStream(FileMode.Create);
        stream.Write(Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" + xml));
    }

    private static string PresentationXml(string slideIds) =>
        $"<p:presentation {Namespaces}>" +
        "<p:sldMasterIdLst><p:sldMasterId id=\"2147483648\" r:id=\"rIdMaster\"/></p:sldMasterIdLst>" +
        $"<p:sldIdLst>{slideIds}</p:sldIdLst>" +
        "<p:sldSz cx=\"12192000\" cy=\"6858000\"/><p:notesSz cx=\"6858000\" cy=\"9144000\"/>" +
        "</p:presentation>";

    private static string SlideXml(SyntheticSlide slide) =>
        $"<p:sld {Namespaces}{(slide.Hidden ? " show=\"0\"" : "")}><p:cSld><p:spTree>{EmptyShapeTree}" +
        "<p:sp><p:nvSpPr><p:cNvPr id=\"2\" name=\"Title\"/><p:cNvSpPr txBox=\"1\"/><p:nvPr/></p:nvSpPr>" +
        "<p:spPr><a:xfrm><a:off x=\"914400\" y=\"914400\"/><a:ext cx=\"10363200\" cy=\"1371600\"/></a:xfrm>" +
        "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
        $"<p:txBody><a:bodyPr/><a:p><a:r><a:rPr lang=\"es-CR\" sz=\"4400\"/><a:t>{Escape(slide.Title)}</a:t></a:r></a:p></p:txBody>" +
        "</p:sp></p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>";

    /// <summary>
    /// Like PowerPoint's notes page: a body placeholder with the text, one paragraph per line,
    /// and a slide-number placeholder that must not end up in the notes.
    /// </summary>
    private static string NotesXml(string notes)
    {
        var paragraphs = string.Concat(notes.Split('\n').Select(line =>
            line.Length == 0 ? "<a:p/>" : $"<a:p><a:r><a:rPr lang=\"es-CR\"/><a:t>{Escape(line)}</a:t></a:r></a:p>"));

        return
            $"<p:notes {Namespaces}><p:cSld><p:spTree>{EmptyShapeTree}" +
            "<p:sp><p:nvSpPr><p:cNvPr id=\"2\" name=\"Notes\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"body\" idx=\"1\"/></p:nvPr></p:nvSpPr>" +
            $"<p:spPr/><p:txBody><a:bodyPr/>{paragraphs}</p:txBody></p:sp>" +
            "<p:sp><p:nvSpPr><p:cNvPr id=\"3\" name=\"Number\"/><p:cNvSpPr/><p:nvPr><p:ph type=\"sldNum\" sz=\"quarter\" idx=\"5\"/></p:nvPr></p:nvSpPr>" +
            "<p:spPr/><p:txBody><a:bodyPr/><a:p><a:fld id=\"{B6F15528-21DE-4FAA-801E-634DDDAF4B2B}\" type=\"slidenum\"><a:t>7</a:t></a:fld></a:p></p:txBody></p:sp>" +
            "</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:notes>";
    }

    private static string Escape(string text) => SecurityElement.Escape(text);

    private static string MasterXml =>
        $"<p:sldMaster {Namespaces}><p:cSld><p:bg><p:bgPr><a:solidFill><a:srgbClr val=\"FFFFFF\"/></a:solidFill><a:effectLst/></p:bgPr></p:bg>" +
        $"<p:spTree>{EmptyShapeTree}</p:spTree></p:cSld>" +
        "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\" " +
        "accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
        "<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"rIdLayout\"/></p:sldLayoutIdLst></p:sldMaster>";

    private static string LayoutXml =>
        $"<p:sldLayout {Namespaces} type=\"blank\"><p:cSld name=\"Blank\"><p:spTree>{EmptyShapeTree}</p:spTree></p:cSld>" +
        "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sldLayout>";

    private static string ThemeXml
    {
        get
        {
            const string fill = "<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>";
            const string line = "<a:ln w=\"9525\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:ln>";
            const string effect = "<a:effectStyle><a:effectLst/></a:effectStyle>";
            const string font = "<a:latin typeface=\"Arial\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/>";
            return
                "<a:theme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" name=\"Synthetic\"><a:themeElements>" +
                "<a:clrScheme name=\"Synthetic\">" +
                "<a:dk1><a:srgbClr val=\"000000\"/></a:dk1><a:lt1><a:srgbClr val=\"FFFFFF\"/></a:lt1>" +
                "<a:dk2><a:srgbClr val=\"262A30\"/></a:dk2><a:lt2><a:srgbClr val=\"F5F7F6\"/></a:lt2>" +
                "<a:accent1><a:srgbClr val=\"1F4FB8\"/></a:accent1><a:accent2><a:srgbClr val=\"FFE45C\"/></a:accent2>" +
                "<a:accent3><a:srgbClr val=\"D7263D\"/></a:accent3><a:accent4><a:srgbClr val=\"59606B\"/></a:accent4>" +
                "<a:accent5><a:srgbClr val=\"C9D6EA\"/></a:accent5><a:accent6><a:srgbClr val=\"163A88\"/></a:accent6>" +
                "<a:hlink><a:srgbClr val=\"1F4FB8\"/></a:hlink><a:folHlink><a:srgbClr val=\"163A88\"/></a:folHlink>" +
                "</a:clrScheme>" +
                $"<a:fontScheme name=\"Synthetic\"><a:majorFont>{font}</a:majorFont><a:minorFont>{font}</a:minorFont></a:fontScheme>" +
                "<a:fmtScheme name=\"Synthetic\">" +
                $"<a:fillStyleLst>{fill}{fill}{fill}</a:fillStyleLst>" +
                $"<a:lnStyleLst>{line}{line}{line}</a:lnStyleLst>" +
                $"<a:effectStyleLst>{effect}{effect}{effect}</a:effectStyleLst>" +
                $"<a:bgFillStyleLst>{fill}{fill}{fill}</a:bgFillStyleLst>" +
                "</a:fmtScheme></a:themeElements></a:theme>";
        }
    }
}
