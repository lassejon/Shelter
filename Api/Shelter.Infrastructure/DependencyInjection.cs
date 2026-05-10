using App.Auth;
using App.Common;
using App.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shelter.Domain.Users;
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
        services.AddSingleton<IClock, SystemClock>();

        services.AddDbContext<ShelterDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsql => npgsql.UseNetTopologySuite());

            var env = sp.GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        services.AddScoped<IShelterDbContext>(sp => sp.GetRequiredService<ShelterDbContext>());

        // Identity is wired BEFORE JwtSettings so that AddJwtBearer (in JwtSettings.OnConfigure)
        // can install JwtBearer as the default authenticate / challenge / forbid scheme on top
        // of Identity's cookie schemes. AddIdentity's cookie schemes stay registered but unused
        // — we authenticate via Bearer tokens only.
        services.Configure<IdentityOptions>(options =>
        {
            options.User.RequireUniqueEmail = true;
        });
        services.AddIdentity<User, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ShelterDbContext>()
            .AddDefaultTokenProviders();

        services.AddSettings<JwtSettings>(configuration);
        services.AddSettings<BlobStorageSettings>(configuration);
        services.AddSettings<CorsSettings>(configuration);

        services.AddScoped<IJwtGenerator, JwtGenerator>();

        return services;
    }
}
