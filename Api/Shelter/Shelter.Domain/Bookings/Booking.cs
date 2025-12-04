using Shelter.Domain.Users;

namespace Shelter.Domain.Bookings;

public enum BookingType
{
    Inclusive = 0,  // counts towards capacity
    Exclusive = 1   // reserves entire shelter
}

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2
}

public class Booking
{
    public Guid Id { get; set; }

    public Guid ShelterId { get; set; }
    public Shelters.Shelter Shelter { get; set; } = null!;

    public Guid BookerId { get; set; }
    public User Booker { get; set; } = null!;

    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }

    public int Guests { get; set; }
    public BookingType Type { get; set; }          // NEW
    public BookingStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
