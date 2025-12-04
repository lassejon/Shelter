namespace Shelter.Domain.Users;

using Microsoft.AspNetCore.Identity;
using Shelter.Domain.Bookings;
using Shelter.Domain.Pictures;
using Shelter.Domain.Shelters;

public class User : IdentityUser<Guid>
{
    public int Index { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public ICollection<Shelter> OwnedShelters { get; set; } = new List<Shelter>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Picture> Pictures { get; set; } = new List<Picture>();
}
