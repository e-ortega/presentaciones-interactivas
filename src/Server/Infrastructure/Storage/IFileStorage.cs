namespace Server.Infrastructure.Storage;

/// <summary>
/// Stores uploaded decks and generated files (slide images, student PDF).
/// Paths are relative and use forward slashes, e.g. "presentations/{id}/deck.pptx".
/// Local disk in development, private Blob Storage in Azure.
/// </summary>
public interface IFileStorage
{
    Task SaveAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Returns null when the file does not exist.</summary>
    Task<Stream?> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>"Local" or "AzureBlob".</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Root folder for the local provider.</summary>
    public string LocalRoot { get; set; } = "storage";

    public string? BlobConnectionString { get; set; }
    public string BlobContainer { get; set; } = "files";
}
