using System.Collections.Concurrent;
using App.Auth;
using Shelter.Domain.Auth;

namespace Shelter.Infrastructure.Auth;

// Dev-only user store. State lives in the process — restart wipes it.
// Seeded with dev@shelter.local / password (ShelterOwner) whose id matches
// the Shelter slice's DevOwnerId, so Create-Shelter calls round-trip cleanly.
internal sealed class InMemoryUserStore : IUserStore
{
    private static readonly Guid SeededUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly ConcurrentDictionary<Guid, StoredUser> _byId = new();
    private readonly ConcurrentDictionary<string, Guid> _byEmail = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryUserStore()
    {
        var dev = new StoredUser(
            Id: SeededUserId,
            Email: "dev@shelter.local",
            Password: "password",
            FirstName: "Dev",
            LastName: "User",
            Roles: new[] { AppRoles.ShelterOwner });

        _byId[dev.Id] = dev;
        _byEmail[dev.Email] = dev.Id;
    }

    public Task<StoredUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (_byEmail.TryGetValue(email, out var id) && _byId.TryGetValue(id, out var user))
        {
            return Task.FromResult<StoredUser?>(user);
        }
        return Task.FromResult<StoredUser?>(null);
    }

    public Task<StoredUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<bool> AddAsync(StoredUser user, CancellationToken cancellationToken = default)
    {
        if (!_byEmail.TryAdd(user.Email, user.Id))
        {
            return Task.FromResult(false);
        }
        _byId[user.Id] = user;
        return Task.FromResult(true);
    }

    public Task AddRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        _byId.AddOrUpdate(
            userId,
            _ => throw new InvalidOperationException($"User {userId} not found"),
            (_, existing) => existing.Roles.Contains(role)
                ? existing
                : existing with { Roles = [.. existing.Roles, role] });
        return Task.CompletedTask;
    }
}
