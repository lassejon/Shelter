using Shelter.Infrastructure.Settings.Base;

namespace Shelter.Infrastructure.Settings;

public class CorsSettings : Settings<CorsSettings>
{
    public const string PolicyName = "ClientPermission";

    public string[] AllowedOrigins { get; init; } = [];

    public override IServiceCollection OnConfigure(IServiceCollection services)
    {
        var origins = AllowedOrigins;

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
