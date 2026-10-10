using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Server.Infrastructure.Storage;

/// <summary>Private container; files are served through the app, never by public URL.</summary>
public class BlobFileStorage(BlobContainerClient container) : IFileStorage
{
    public async Task SaveAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var options = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } };
        await container.GetBlobClient(path).UploadAsync(content, options, cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            return await container.GetBlobClient(path).OpenReadAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        await container.GetBlobClient(path).DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
