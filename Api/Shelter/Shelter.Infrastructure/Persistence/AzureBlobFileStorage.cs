using Shelter.Application.Interfaces;

namespace Shelter.Infrastructure.Persistence;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public class AzureBlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobFileStorage(string connectionString, string containerName)
    {
        var serviceClient = new BlobServiceClient(connectionString);
        _container = serviceClient.GetBlobContainerClient(containerName);

        _container.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<string> SaveShelterPictureAsync(Guid shelterId, Stream content, string contentType, CancellationToken ct = default)
    {
        var blobName = $"shelters/{shelterId}/{Guid.NewGuid()}";

        var blobClient = _container.GetBlobClient(blobName);

        await blobClient.UploadAsync(content, new BlobHttpHeaders
        {
            ContentType = contentType
        }, cancellationToken: ct);
        
        return blobClient.Uri.ToString();
    }

    public async Task DeleteShelterPictureAsync(string urlOrKey, CancellationToken ct = default)
    {
        // if you store full URL, extract the path part after container
        // or instead store `blobName` separately.
        // Simplest: treat urlOrKey as blobName directly.
        var blobClient = _container.GetBlobClient(urlOrKey);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
