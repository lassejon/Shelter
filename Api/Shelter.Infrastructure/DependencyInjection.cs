using App.Auth;
using App.Common;
using App.Shelters;
using Microsoft.Extensions.Configuration;
using Shelter.Infrastructure.Auth;
using Shelter.Infrastructure.Common;
using Shelter.Infrastructure.Settings;
using Shelter.Infrastructure.Settings.Base;
using Shelter.Infrastructure.Shelters;

namespace Shelter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettings<JwtSettings>(configuration);

        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<IUserStore, InMemoryUserStore>();
        services.AddScoped<IJwtGenerator, JwtGenerator>();

        services.AddSingleton<IShelterRepository, InMemoryShelterRepository>();

        return services;
    }
}
