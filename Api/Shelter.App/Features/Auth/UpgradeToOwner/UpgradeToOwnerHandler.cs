using App.Auth;
using App.Features.Auth.Shared;
using Microsoft.AspNetCore.Identity;
using Shelter.Domain.Auth;
using Shelter.Domain.Common;
using Shelter.Domain.Users;

namespace App.Features.Auth.UpgradeToOwner;

public sealed class UpgradeToOwnerHandler(
    UserManager<User> userManager,
    IJwtGenerator jwtGenerator,
    ILogger<UpgradeToOwnerHandler> logger)
{
    public async Task<AuthResponse> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new DomainNotFoundException("User not found.");

        if (!await userManager.IsInRoleAsync(user, AppRoles.ShelterOwner))
        {
            var addResult = await userManager.AddToRoleAsync(user, AppRoles.ShelterOwner);
            if (!addResult.Succeeded)
            {
                throw new DomainValidationException(
                    string.Join(" ", addResult.Errors.Select(e => e.Description)));
            }
            logger.LogInformation("User {UserId} upgraded to ShelterOwner", user.Id);
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (token, expiresAtUtc) = jwtGenerator.GenerateToken(user.Id, user.Email!, roles);

        return new AuthResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            roles,
            token,
            expiresAtUtc);
    }
}
