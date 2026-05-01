namespace App.Auth;

public interface IUserStore
{
    Task<StoredUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<StoredUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Returns false if the email is already taken.
    Task<bool> AddAsync(StoredUser user, CancellationToken cancellationToken = default);

    Task AddRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
}
