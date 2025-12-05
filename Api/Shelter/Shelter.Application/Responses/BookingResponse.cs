using Shelter.Domain.Bookings;

namespace Shelter.Application.Responses;

public record BookingResponse(
    Guid Id,
    Guid ShelterId,
    Guid BookerId,
    string BookerName,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int Guests,
    BookingType Type,
    BookingStatus Status,
    DateTimeOffset CreatedAt)
{
    public static BookingResponse FromDomain(Booking booking) => new(
        booking.Id,
        booking.ShelterId,
        booking.BookerId,
        booking.Booker.FirstName,
        booking.StartUtc,
        booking.EndUtc,
        booking.Guests,
        booking.Type,
        booking.Status,
        booking.CreatedAt);
}
