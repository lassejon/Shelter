using App.Common;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Shelter.Infrastructure.Settings.Base;
using Shelter.Infrastructure.Storage;

namespace Shelter.Infrastructure.Settings;

internal class BlobStorageSettings : Settings<BlobStorageSettings>
{
    public string ConnectionString { get; init; } = null!;
    public string ContainerName { get; init; } = null!;

    public override IServiceCollection OnConfigure(IServiceCollection services)
    {
        var connectionString = ConnectionString;
        var containerName = ContainerName;

        services.AddSingleton(_ =>
        {
            var serviceClient = new BlobServiceClient(connectionString);
            var container = serviceClient.GetBlobContainerClient(containerName);
            container.CreateIfNotExists(PublicAccessType.Blob);
            return container;
        });

        services.AddSingleton<IFileStorage, AzureBlobFileStorage>();

        return services;
    }
}
