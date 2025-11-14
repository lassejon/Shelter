using Microsoft.Extensions.DependencyInjection;
using Shelter.Application.Interfaces;
using Shelter.Domain.Users;
using Shelter.Infrastructure.Configuration.Extensions;
using Shelter.Infrastructure.Persistence;
using Shelter.Infrastructure.Persistence.Users;
using Shelter.Infrastructure.Settings;

namespace Shelter.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.ConfigureSettings<DatabaseSettings>();

        services.AddScoped<IUnitOfWork>(s => s.GetRequiredService<ShelterDbContext>());
        services.AddScoped<IEntityRepository<User>, UserRepository>();

        return services;
    }
}