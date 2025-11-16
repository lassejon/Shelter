using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shelter.Infrastructure.Persistence;
using Shelter.Infrastructure.Settings.Base;

namespace Shelter.Infrastructure.Settings;

internal class DatabaseSettings : Settings<DatabaseSettings>
{
    private string ConnectionString { get; set; } = null!;

    public override IServiceCollection OnConfigure(IServiceCollection services)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        services = base.OnConfigure(services);
        
        ConnectionString = configuration[$"{SectionName}:{nameof(ConnectionString)}"]!;
        
        services.AddDbContext<ShelterDbContext>(options =>
            options.UseNpgsql(ConnectionString, o =>
            {
                o.UseNetTopologySuite();
            }));

        return services;
    }
}