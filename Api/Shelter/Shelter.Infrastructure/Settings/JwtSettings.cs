using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Shelter.Infrastructure.Settings;
using Base;

public class JwtSettings : Settings<JwtSettings>
{
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public string Secret { get; init; } = null!;
    public int AccessTokenMinutes { get; init; } = 60;

    public override IServiceCollection OnConfigure(IServiceCollection serviceCollection)
    {
        serviceCollection
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
                    ValidAudience = Audience,
                    ValidIssuer = Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret))
                };
            
                // options.Events = new JwtBearerEvents
                // {
                //     OnMessageReceived = context =>
                //     {
                //         var accessToken = context.Request.Query["access_token"];
                //
                //         // If the request is for our hub...
                //         var path = context.HttpContext.Request.Path;
                //         if (!string.IsNullOrEmpty(accessToken) &&
                //             (path.StartsWithSegments("/chat")))
                //         {
                //             // Read the token out of the query string
                //             context.Token = accessToken;
                //         }
                //         return Task.CompletedTask;
                //     }
                // };
            });

        const string clientPermission = "ClientPermission";

        serviceCollection.AddCors(options =>
        {
            options.AddPolicy(clientPermission, policy =>
            {
                policy.WithOrigins("https://localhost:3000")
                    .SetIsOriginAllowed((host) => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return serviceCollection;
    }
}
