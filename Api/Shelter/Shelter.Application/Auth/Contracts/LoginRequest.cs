namespace Shelter.Application.Auth.Contracts;

public sealed class LoginRequest
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
}