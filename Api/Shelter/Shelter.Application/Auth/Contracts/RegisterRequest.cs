namespace Shelter.Application.Auth.Contracts;

public class RegisterRequest
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public required string FirstName { get; init; } = null!;
    public string? MiddleName { get; init; }
    public required string LastName { get; init; } = null!;
}