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
    }
}