using Shelter.Domain.Bookings;

namespace App.Features.Bookings.Create;

public class CreateBookingRequest
{
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public int Guests { get; set; }
    public BookingType Type { get; set; }
}
