using App.Auth;
using App.Common;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shelter.Infrastructure.Auth;
using Shelter.Infrastructure.Common;
using Shelter.Infrastructure.Persistence;
using Shelter.Infrastructure.Settings;
using Shelter.Infrastructure.Settings.Base;

namespace Shelter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettings<JwtSettings>(configuration);
        services.AddSettings<BlobStorageSettings>(configuration);

        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<IUserStore, InMemoryUserStore>();
        services.AddScoped<IJwtGenerator, JwtGenerator>();

        services.AddDbContext<ShelterDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"));

            var env = sp.GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        services.AddScoped<IShelterDbContext>(sp => sp.GetRequiredService<ShelterDbContext>());

        return services;
    }
}
