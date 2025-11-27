using NetTopologySuite.Geometries;
using Shelter.Domain.Bookings;
using Shelter.Domain.Users;

namespace Shelter.Domain.Shelters;

public enum ShelterBookingPolicy
{
    ExclusiveOnly = 0,
    InclusiveOnly = 1,
    Both = 2
}

public class Shelter
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public int Capacity { get; set; }
    public ShelterBookingPolicy BookingPolicy { get; set; }

    public Point Location { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ShelterPicture> Pictures { get; set; } = new List<ShelterPicture>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}