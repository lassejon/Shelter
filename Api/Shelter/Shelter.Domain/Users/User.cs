namespace Shelter.Domain.Users;

using Microsoft.AspNetCore.Identity;

public class User : IdentityUser<Guid>
{
    public int Index { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}