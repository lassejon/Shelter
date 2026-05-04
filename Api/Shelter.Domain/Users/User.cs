using Microsoft.AspNetCore.Identity;

namespace Shelter.Domain.Users;

public class User : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string DisplayName =>
        string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)))
            is { Length: > 0 } combined
            ? combined
            : UserName ?? Email ?? "User";
}
