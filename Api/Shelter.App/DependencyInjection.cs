using App.Features.Auth.Login;
using App.Features.Auth.Register;
using App.Features.Shelters.Create;

namespace App;

public static class DependencyInjection
{
    public static IServiceCollection AddShelterApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateShelterHandler>();
        return services;
    }

    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        return services;
    }
}
