using App.Auth;
using App.Features.Auth.Shared;
using Shelter.Domain.Auth;

namespace App.Features.Auth.Register;

public enum RegisterFailure
{
    EmailAlreadyExists,
}

public sealed class RegisterHandler(
    IUserStore userStore,
    IJwtGenerator jwtGenerator,
    ILogger<RegisterHandler> logger)
{
    public async Task<(AuthResponse? response, RegisterFailure? failure)> HandleAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var roles = request.IsShelterOwner
            ? new[] { AppRoles.ShelterOwner }
            : Array.Empty<string>();

        var user = new StoredUser(
            Id: Guid.NewGuid(),
            Email: request.Email,
            Password: request.Password,
            FirstName: request.FirstName,
            LastName: request.LastName,
            Roles: roles);

        var created = await userStore.AddAsync(user, cancellationToken);
        if (!created)
        {
            logger.LogInformation("Register failed: email already exists ({Email})", request.Email);
            return (null, RegisterFailure.EmailAlreadyExists);
        }

        var (token, expiresAtUtc) = jwtGenerator.GenerateToken(user.Id, user.Email, user.Roles);

        logger.LogInformation("Registered user {UserId} with roles {Roles}", user.Id, user.Roles);

        var response = new AuthResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles,
            token,
            expiresAtUtc);

        return (response, null);
    }
}
