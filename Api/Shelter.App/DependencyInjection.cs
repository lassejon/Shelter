using App.Features.Shelters.Create;

namespace App;

public static class DependencyInjection
{
    public static IServiceCollection AddShelterApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateShelterHandler>();
        return services;
    }
}
