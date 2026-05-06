using App.Common;
using App.Features.Auth.Login;
using App.Features.Auth.Me;
using App.Features.Auth.Register;
using App.Features.Auth.UpgradeToOwner;
using App.Features.Bookings.Cancel;
using App.Features.Bookings.Create;
using App.Features.Bookings.Get;
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
using App.Features.Shelters.Update;

namespace App;

public static class DependencyInjection
{
    public static IServiceCollection AddShelterApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateShelterHandler>();
        services.AddScoped<GetShelterHandler>();
        services.AddScoped<SearchShelterHandler>();
        services.AddScoped<UpdateShelterHandler>();
        services.AddScoped<DeleteShelterHandler>();
        services.AddScoped<AssetOrphanRecovery>();
        return services;
    }

    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<UpgradeToOwnerHandler>();
        services.AddScoped<MeHandler>();
        return services;
    }

    public static IServiceCollection AddBookingApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateBookingHandler>();
        services.AddScoped<GetBookingHandler>();
        services.AddScoped<SearchBookingByBookerHandler>();
        services.AddScoped<SearchBookingByShelterHandler>();
        services.AddScoped<CancelBookingHandler>();
        return services;
    }

    public static IServiceCollection AddReviewApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateReviewHandler>();
        services.AddScoped<GetReviewHandler>();
        services.AddScoped<GetMyReviewHandler>();
        services.AddScoped<SearchReviewByShelterHandler>();
        services.AddScoped<SearchReviewPictureHandler>();
        services.AddScoped<UpdateReviewHandler>();
        services.AddScoped<DeleteReviewHandler>();
        return services;
    }
}
