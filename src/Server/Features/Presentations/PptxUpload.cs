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
    public static UploadError? Validate(string fileName, string localPath, ImportOptions options)
    {
        if (!string.Equals(Path.GetExtension(fileName), ".pptx", StringComparison.OrdinalIgnoreCase))
        {
            return UploadError.NotPptx;
        }

        var length = new FileInfo(localPath).Length;
        if (length > options.MaxFileBytes)
        {
            return UploadError.TooLarge;
        }

        if (!HasZipSignature(localPath))
        {
            return UploadError.Unreadable;
        }

        int visibleSlides;
        try
        {
            visibleSlides = PptxReader.ReadVisibleSlideNotes(localPath).Count;
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

    /// <summary>The file name without extension, as the initial title.</summary>
    public static string TitleFrom(string fileName)
    {
        var title = Path.GetFileNameWithoutExtension(fileName).Trim();
        return title.Length <= 200 ? title : title[..200];
    }

    private static bool HasZipSignature(string path)
    {
        Span<byte> header = stackalloc byte[4];
        using var file = File.OpenRead(path);
        return file.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) == header.Length
            && header.SequenceEqual(ZipSignature);
    }
}
