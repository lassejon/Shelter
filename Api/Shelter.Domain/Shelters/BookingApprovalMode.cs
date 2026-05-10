namespace Shelter.Domain.Shelters;

/// <summary>
/// Per-shelter setting for how new bookings transition into the calendar.
/// <c>Instant</c> mirrors the original auto-confirm flow (booking is immediately
/// <c>Confirmed</c>); <c>RequiresApproval</c> queues the booking as <c>Pending</c>
/// until the shelter owner approves or rejects it.
/// </summary>
public enum BookingApprovalMode
{
    Instant = 0,
    RequiresApproval = 1,
}
