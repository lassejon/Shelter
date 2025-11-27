using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shelter.Application.Interfaces;
using Shelter.Domain.Users;
using Shelter.Infrastructure.Auth;
using Shelter.Infrastructure.Configuration.Extensions;
using Shelter.Infrastructure.Persistence;
using Shelter.Infrastructure.Persistence.Users;
using Shelter.Infrastructure.Settings;
using Shelter.Infrastructure.Settings.Base;

namespace Shelter.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,  ConfigurationManager configuration)
    {
        services.AddSettings<DatabaseSettings>(configuration);
        services.AddSettings<JwtSettings>(configuration);
        services.AddScoped<IJwtGenerator, JwtGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        
        services.AddScoped<IUnitOfWork>(s => s.GetRequiredService<ShelterDbContext>());
        services.AddScoped<IEntityRepository<User>, UserRepository>();

        return services;
    }
}