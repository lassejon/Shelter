using App.Auth;
using App.Features.Auth.Shared;
using Microsoft.AspNetCore.Identity;
using Shelter.Domain.Common;
using Shelter.Domain.Users;

namespace App.Features.Auth.Me;

public sealed class MeHandler(
    UserManager<User> userManager,
    IJwtGenerator jwtGenerator)
{
    public async Task<AuthResponse> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new DomainNotFoundException("User not found.");

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
