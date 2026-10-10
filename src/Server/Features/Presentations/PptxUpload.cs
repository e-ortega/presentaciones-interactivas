using Server.Features.Import;

namespace Server.Features.Presentations;

public enum UploadError
{
    NoFile,

    /// <summary>Not a .pptx: includes .ppt and .pptm (macros are never accepted).</summary>
    NotPptx,
    TooLarge,

    /// <summary>Named .pptx but it is not a presentation PowerPoint could open.</summary>
    Unreadable,
    NoVisibleSlides,
    TooManySlides,
}

/// <summary>Checks an uploaded file before anything is stored or handed to LibreOffice.</summary>
public static class PptxUpload
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";

    private static ReadOnlySpan<byte> ZipSignature => [0x50, 0x4B, 0x03, 0x04]; // "PK\x03\x04"

    /// <summary>Returns null when the file can be imported.</summary>
    /// <param name="content">A seekable stream. It is left open, at an unspecified position.</param>
    public static UploadError? Validate(string fileName, Stream content, ImportOptions options)
    {
        if (!string.Equals(Path.GetExtension(fileName), ".pptx", StringComparison.OrdinalIgnoreCase))
        {
            return UploadError.NotPptx;
        }

        if (content.Length > options.MaxFileBytes)
        {
            return UploadError.TooLarge;
        }

        if (!HasZipSignature(content))
        {
            return UploadError.Unreadable;
        }

        int visibleSlides;
        try
        {
            content.Position = 0;
            visibleSlides = PptxReader.ReadVisibleSlideNotes(content).Count;
        }
        catch (InvalidDataException)
        {
            return UploadError.Unreadable;
        }

        return visibleSlides switch
        {
            0 => UploadError.NoVisibleSlides,
            _ when visibleSlides > options.MaxSlides => UploadError.TooManySlides,
            _ => null,
        };
    }

    /// <summary>
    /// The file name without folders or extension, as the initial title. Browsers send only
    /// the name, but other clients may send a full path with either kind of separator.
    /// </summary>
    public static string TitleFrom(string fileName)
    {
        var name = fileName[(fileName.LastIndexOfAny(['/', '\\']) + 1)..].Trim();
        var title = Path.GetFileNameWithoutExtension(name).Trim();
        if (title.Length == 0)
        {
            title = name; // a file called just ".pptx" keeps that as its title
        }

        return title.Length <= 200 ? title : title[..200];
    }

    private static bool HasZipSignature(Stream content)
    {
        Span<byte> header = stackalloc byte[4];
        content.Position = 0;
        return content.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) == header.Length
            && header.SequenceEqual(ZipSignature);
    }
}
