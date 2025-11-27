using Shelter.Domain.Users;

namespace Shelter.Application.Interfaces;

public interface IJwtGenerator
{
    (string token, DateTime expiresAtUtc) GenerateToken(User user);
}