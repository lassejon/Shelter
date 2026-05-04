using App.Auth;
using App.Features.Auth.Shared;
using Microsoft.AspNetCore.Identity;
using Shelter.Domain.Auth;
using Shelter.Domain.Users;

namespace App.Features.Auth.Register;

public enum RegisterFailure
{
    EmailAlreadyExists,
    InvalidPassword,
}

public sealed class RegisterHandler(
    UserManager<User> userManager,
    IJwtGenerator jwtGenerator,
    ILogger<RegisterHandler> logger)
{
    public async Task<(AuthResponse? response, RegisterFailure? failure, IReadOnlyList<string>? errors)> HandleAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            logger.LogInformation("Register failed: email already exists ({Email})", request.Email);
            return (null, RegisterFailure.EmailAlreadyExists, null);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            var errors = create.Errors.Select(e => e.Description).ToList();
            logger.LogInformation("Register failed for {Email}: {Errors}", request.Email, string.Join("; ", errors));
            return (null, RegisterFailure.InvalidPassword, errors);
        }

        if (request.IsShelterOwner)
        {
            await userManager.AddToRoleAsync(user, AppRoles.ShelterOwner);
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (token, expiresAtUtc) = jwtGenerator.GenerateToken(user.Id, user.Email!, roles);

        logger.LogInformation("Registered user {UserId} with roles {Roles}", user.Id, roles);

        var response = new AuthResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            roles,
            token,
            expiresAtUtc);

        return (response, null, null);
    }
}
