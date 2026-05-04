using Shelter.Domain.Bookings;

namespace App.Features.Bookings.Shared;

public record BookingDetailResponse(
    Guid Id,
    Guid ShelterId,
    Guid BookerId,
    string? BookerName,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int Guests,
    BookingType Type,
    BookingStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static BookingDetailResponse FromDomain(Booking booking) => new(
        booking.Id,
        booking.ShelterId,
        booking.BookerId,
        booking.Booker?.DisplayName,
        booking.StartUtc,
        booking.EndUtc,
        booking.Guests,
        booking.Type,
        booking.Status,
        booking.CreatedAt,
        booking.UpdatedAt);
}
