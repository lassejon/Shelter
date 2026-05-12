using App.Auth;
using App.Common;
using App.Features.Auth.Login;
using App.Features.Auth.Me;
using App.Features.Auth.Register;
using App.Features.Auth.UpgradeToOwner;
using App.Features.Bookings.Approve;
using App.Features.Bookings.Cancel;
using App.Features.Bookings.Create;
using App.Features.Bookings.Get;
using App.Features.Bookings.SearchAvailability;
using App.Features.Bookings.SearchByBooker;
using App.Features.Bookings.SearchByShelter;
using App.Features.Reviews.Create;
using App.Features.Reviews.Delete;
using App.Features.Reviews.Get;
using App.Features.Reviews.GetMine;
using App.Features.Reviews.SearchByShelter;
using App.Features.Reviews.SearchPicture;
using App.Features.Reviews.Update;
using App.Features.Shelters.Create;
using App.Features.Shelters.Delete;
using App.Features.Shelters.Get;
using App.Features.Shelters.Search;
using App.Features.Shelters.SearchByOwner;
using App.Features.Shelters.Update;
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

        AddApplicationHandlers(services);

        return services;
    }

    private static void AddApplicationHandlers(IServiceCollection services)
    {
        // Shelters
        services.AddScoped<CreateShelterHandler>();
        services.AddScoped<GetShelterHandler>();
        services.AddScoped<SearchShelterHandler>();
        services.AddScoped<SearchShelterByOwnerHandler>();
        services.AddScoped<UpdateShelterHandler>();
        services.AddScoped<DeleteShelterHandler>();

        // Auth
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<UpgradeToOwnerHandler>();
        services.AddScoped<MeHandler>();

        // Bookings
        services.AddScoped<CreateBookingHandler>();
        services.AddScoped<GetBookingHandler>();
        services.AddScoped<SearchBookingAvailabilityHandler>();
        services.AddScoped<SearchBookingByBookerHandler>();
        services.AddScoped<SearchBookingByShelterHandler>();
        services.AddScoped<ApproveBookingHandler>();
        services.AddScoped<CancelBookingHandler>();

        // Reviews
        services.AddScoped<CreateReviewHandler>();
        services.AddScoped<GetReviewHandler>();
        services.AddScoped<GetMyReviewHandler>();
        services.AddScoped<SearchReviewByShelterHandler>();
        services.AddScoped<SearchReviewPictureHandler>();
        services.AddScoped<UpdateReviewHandler>();
        services.AddScoped<DeleteReviewHandler>();

        // Cross-aggregate App utilities
        services.AddScoped<AssetOrphanRecovery>();
    }
}
