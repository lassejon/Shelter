using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shelter.Infrastructure.Persistence;
using Shelter.Infrastructure.Settings.Base;

namespace Shelter.Infrastructure.Settings;

internal class DatabaseSettings : Settings<DatabaseSettings>
{
    public string ConnectionString { get; init; } = null!;

    public override IServiceCollection OnConfigure(IServiceCollection services)
    {
        services.AddDbContext<ShelterDbContext>(options =>
            options.UseNpgsql(ConnectionString, o =>
            {
                o.UseNetTopologySuite();
            }));

        return services;
    }
}