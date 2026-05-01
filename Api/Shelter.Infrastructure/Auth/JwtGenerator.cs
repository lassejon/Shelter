using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using App.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shelter.Infrastructure.Settings;

namespace Shelter.Infrastructure.Auth;

internal sealed class JwtGenerator(IOptions<JwtSettings> options) : IJwtGenerator
{
    private readonly JwtSettings _settings = options.Value;

    public (string token, DateTimeOffset expiresAtUtc) GenerateToken(
        Guid userId,
        string email,
        IEnumerable<string> roles)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var expires = utcNow.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: utcNow.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return (encoded, expires);
    }
}
