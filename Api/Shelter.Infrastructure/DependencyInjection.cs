using App.Auth;
using App.Common;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<IUserStore, InMemoryUserStore>();
        services.AddScoped<IJwtGenerator, JwtGenerator>();

        services.AddDbContext<ShelterDbContext>(options =>
            options.UseInMemoryDatabase("Shelter"));

        services.AddScoped<IShelterDbContext>(sp => sp.GetRequiredService<ShelterDbContext>());

        return services;
    }
}
