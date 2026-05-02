using App.Common;
using App.Features.Auth.Login;
using App.Features.Auth.Register;
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
        return services;
    }
}
