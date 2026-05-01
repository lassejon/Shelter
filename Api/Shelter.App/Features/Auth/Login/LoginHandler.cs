using App.Auth;
using App.Features.Auth.Shared;

namespace App.Features.Auth.Login;

public sealed class LoginHandler(
    IUserStore userStore,
    IJwtGenerator jwtGenerator,
    ILogger<LoginHandler> logger)
{
    public async Task<AuthResponse?> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userStore.FindByEmailAsync(request.Email, cancellationToken);

        if (user is null || !string.Equals(user.Password, request.Password, StringComparison.Ordinal))
        {
            logger.LogInformation("Login failed for {Email}", request.Email);
            return null;
        }

        var (token, expiresAtUtc) = jwtGenerator.GenerateToken(user.Id, user.Email, user.Roles);

        logger.LogInformation("Login succeeded for {UserId}", user.Id);

        return new AuthResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles,
            token,
            expiresAtUtc);
    }
}
