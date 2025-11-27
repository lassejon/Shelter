namespace Shelter.Application.Auth.Contracts;

public sealed class AuthResponse
{
    public required string AccessToken { get; init; } = null!;
    public required DateTime ExpiresAtUtc { get; init; }
    
    // public string RefreshToken { get; init; } = default!;
}