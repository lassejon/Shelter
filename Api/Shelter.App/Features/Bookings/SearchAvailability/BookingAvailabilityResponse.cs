using Shelter.Domain.Bookings;

namespace App.Features.Bookings.SearchAvailability;

public sealed record BookingAvailabilityResponse(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int Guests,
    BookingType Type,
    BookingStatus Status)
{
    public static BookingAvailabilityResponse FromDomain(Booking booking) => new(
        booking.StartUtc,
        booking.EndUtc,
        booking.Guests,
        booking.Type,
        booking.Status);
}
