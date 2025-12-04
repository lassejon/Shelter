using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shelter.Application.Interfaces;
using Shelter.Domain.Auth;
using Shelter.Domain.Users;
using Shelter.Infrastructure.Auth;
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
        services.AddSettings<StorageSettings>(configuration);
        
        services.AddScoped<IJwtGenerator, JwtGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        
        services.AddScoped<IUnitOfWork>(s => s.GetRequiredService<ShelterDbContext>());
        services.AddScoped<IEntityRepository<User>, UserRepository>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(nameof(AppScopes.SheltersRead), p =>
                p.RequireClaim("scope", AppScopes.SheltersRead));

            options.AddPolicy(nameof(AppScopes.SheltersManage), p =>
                p.RequireClaim("scope", AppScopes.SheltersManage));

            options.AddPolicy(nameof(AppScopes.BookingsWrite), p =>
                p.RequireClaim("scope", AppScopes.BookingsWrite));

            options.AddPolicy(nameof(AppScopes.BookingsManage), p =>
                p.RequireClaim("scope", AppScopes.BookingsManage));
        });

        return services;
    }
}