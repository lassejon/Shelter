using App.Auth;
using App.Features.Auth.Shared;
using Microsoft.AspNetCore.Identity;
using Shelter.Domain.Users;

namespace App.Features.Auth.Login;

public enum LoginFailure
{
    InvalidCredentials,
    EmailNotConfirmed,
}

public sealed class LoginHandler(
    UserManager<User> userManager,
    IJwtGenerator jwtGenerator,
    ILogger<LoginHandler> logger)
{
    public async Task<(AuthResponse? response, LoginFailure? failure)> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            logger.LogInformation("Login failed for {Email}", request.Email);
            return (null, LoginFailure.InvalidCredentials);
        }

        if (!user.EmailConfirmed)
        {
            logger.LogInformation("Login blocked — email not confirmed for {UserId}", user.Id);
            return (null, LoginFailure.EmailNotConfirmed);
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (token, expiresAtUtc) = jwtGenerator.GenerateToken(user.Id, user.Email!, roles);

        logger.LogInformation("Login succeeded for {UserId}", user.Id);

        return (new AuthResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            roles,
            token,
            expiresAtUtc), null);
    }
}
