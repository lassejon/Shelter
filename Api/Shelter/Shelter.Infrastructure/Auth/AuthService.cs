using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shelter.Application.Auth.Contracts;
using Shelter.Application.Interfaces;
using Shelter.Domain.Users;

namespace Shelter.Infrastructure.Auth;

internal sealed class AuthService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IJwtGenerator jwtGenerator)
    : IAuthService
{
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
            return null;

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
            return null; // controller will map this to 401

        var (token, expiresAtUtc) = jwtGenerator.GenerateToken(user);

        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}