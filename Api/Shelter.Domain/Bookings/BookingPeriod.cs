namespace Shelter.Domain.Bookings;

/// <summary>
/// The minimum slice of a booking the availability sweepline cares about — a window, a guest count,
/// and a type. Projected from EF Core in handlers; constructed directly by domain logic and tests.
/// Domain layer rather than App layer because <see cref="Shelters.Shelter"/> consumes it.
/// </summary>
public readonly record struct BookingPeriod(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int Guests,
    BookingType Type);
