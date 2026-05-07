namespace App.Features.Bookings.SearchAvailability;

public sealed class SearchBookingAvailabilityRequest
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}
