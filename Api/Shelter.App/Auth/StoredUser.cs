namespace App.Auth;

// Dev-only shape for the in-memory user store.
public sealed record StoredUser(
    Guid Id,
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> Roles);
