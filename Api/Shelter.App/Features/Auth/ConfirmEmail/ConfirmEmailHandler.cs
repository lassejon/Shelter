using App.Auth;
using App.Features.Auth.Shared;
using Microsoft.AspNetCore.Identity;
using Shelter.Domain.Users;

namespace App.Features.Auth.ConfirmEmail;

public enum ConfirmEmailFailure
{
    UserNotFound,
    InvalidToken,
}

public sealed class ConfirmEmailHandler(
    UserManager<User> userManager,
    IJwtGenerator jwtGenerator,
    ILogger<ConfirmEmailHandler> logger)
{
    public async Task<(AuthResponse? response, ConfirmEmailFailure? failure, IReadOnlyList<string>? errors)> HandleAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            logger.LogInformation("ConfirmEmail: user {UserId} not found", request.UserId);
            return (null, ConfirmEmailFailure.UserNotFound, null);
        }

        if (user.EmailConfirmed)
        {
            // Already confirmed — issue a token so the UI can land the user logged in even if
            // they re-click an old confirmation link.
            return (await BuildResponseAsync(user), null, null);
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            logger.LogInformation(
                "ConfirmEmail failed for {UserId}: {Errors}", user.Id, string.Join("; ", errors));
            return (null, ConfirmEmailFailure.InvalidToken, errors);
        }

        logger.LogInformation("Email confirmed for {UserId}", user.Id);
        return (await BuildResponseAsync(user), null, null);
    }

    private async Task<AuthResponse> BuildResponseAsync(User user)
    {
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
