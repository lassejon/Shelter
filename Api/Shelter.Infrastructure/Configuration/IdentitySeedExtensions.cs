using Microsoft.AspNetCore.Identity;
using Shelter.Domain.Auth;

namespace Shelter.Infrastructure.Configuration;

public static class IdentitySeedExtensions
{
    private static readonly string[] Roles = [AppRoles.ShelterOwner];

    public static async Task SeedRolesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}
