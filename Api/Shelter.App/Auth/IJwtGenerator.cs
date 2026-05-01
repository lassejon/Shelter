namespace App.Auth;

public interface IJwtGenerator
{
    (string token, DateTimeOffset expiresAtUtc) GenerateToken(
        Guid userId,
        string email,
        IEnumerable<string> roles);
}
