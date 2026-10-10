using Azure.Storage.Blobs;

namespace Server.Infrastructure.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();

        switch (options.Provider)
        {
            case "Local":
                services.AddSingleton<IFileStorage>(new LocalFileStorage(options.LocalRoot));
                break;

            case "AzureBlob":
                if (string.IsNullOrWhiteSpace(options.BlobConnectionString))
                {
                    throw new InvalidOperationException("Storage:BlobConnectionString is required for the AzureBlob provider.");
                }

                services.AddSingleton<IFileStorage>(_ =>
                {
                    var container = new BlobContainerClient(options.BlobConnectionString, options.BlobContainer);
                    container.CreateIfNotExists();
                    return new BlobFileStorage(container);
                });
                break;

            default:
                throw new InvalidOperationException($"Unknown Storage:Provider '{options.Provider}'. Use 'Local' or 'AzureBlob'.");
        }

        return services;
    }
}
