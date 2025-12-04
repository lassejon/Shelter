using Microsoft.Extensions.DependencyInjection;
using Shelter.Application.Interfaces;
using Shelter.Infrastructure.Persistence;
using Shelter.Infrastructure.Settings.Base;

namespace Shelter.Infrastructure.Settings;

public class StorageSettings : Settings<StorageSettings>
{
    public string ConnectionString { get; init; } = null!;
    public List<ContainerSettings> Containers { get; set; } = [];


    public override IServiceCollection OnConfigure(IServiceCollection services)
    {
        Containers.ForEach(x =>
        {
            services.AddSingleton<IFileStorage>(sp =>
                new AzureBlobFileStorage(ConnectionString, x.Key));
        });

        return services;
    }
}

public class ContainerSettings
{
    public string Key { get; set; } = string.Empty;
}