namespace App.Features.Bookings.SearchByShelter;

public class SearchBookingByShelterRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
