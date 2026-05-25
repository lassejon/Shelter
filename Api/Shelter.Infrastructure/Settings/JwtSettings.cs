using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shelter.Domain.Auth;
using Shelter.Infrastructure.Settings.Base;

namespace Shelter.Infrastructure.Settings;

internal class JwtSettings : Settings<JwtSettings>
{
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public string Secret { get; init; } = null!;
    public int AccessTokenMinutes { get; init; } = 60;

    public override IServiceCollection OnConfigure(IServiceCollection services)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = Audience,
                    ValidIssuer = Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.CanManageShelters, policy =>
                policy.RequireRole(AppRoles.ShelterOwner));
        });

        return services;
    }
}
