namespace Shelter.Infrastructure.Configuration.Extensions;

using Domain.Users;
using Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Settings.Base;

public static class ServiceCollection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddJwtAuthentication(string? audience, string? issuer, string secret)
        {

            var validAudiences = audience?.Split(";") ?? [];
            var validIssuers = issuer?.Split(";") ?? [];

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
                        ValidAudiences = validAudiences,
                        ValidIssuers = validIssuers,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
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

            services.AddCors(options =>
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

            return services;
        }

        public IServiceCollection ConfigureIdentity()
        {
            services.Configure<IdentityOptions>(options =>
            {
                options.User.RequireUniqueEmail = true;
            });

            services.AddIdentity<User, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<ShelterDbContext>()
                .AddDefaultTokenProviders();

            return services;
        }

        public IServiceCollection ConfigureSettings<T>() where T : Settings<T>
        {
            var settings = Activator.CreateInstance<T>();
        
            services = settings.OnConfigure(services);

            return services;
        }
    }
}