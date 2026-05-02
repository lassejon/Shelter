using App.Common;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Shelter.Infrastructure.Storage;

public sealed class AzureBlobFileStorage(BlobContainerClient container) : IFileStorage
{
    public async Task UploadAsync(
        string blobKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var blobClient = container.GetBlobClient(blobKey);
        await blobClient.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string blobKey, CancellationToken cancellationToken = default)
    {
        var blobClient = container.GetBlobClient(blobKey);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public string GetPublicUrl(string blobKey) =>
        container.GetBlobClient(blobKey).Uri.ToString();
}
