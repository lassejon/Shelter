namespace App.Features.Auth.Shared;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> Roles,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);
