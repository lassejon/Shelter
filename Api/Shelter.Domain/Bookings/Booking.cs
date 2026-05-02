using Shelter.Domain.Common;

namespace Shelter.Domain.Bookings;

public class Booking
{
    private Booking() { }

    public Guid Id { get; private set; }
    public Guid ShelterId { get; private set; }
    public Guid BookerId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }
    public DateTimeOffset EndUtc { get; private set; }

    public int Guests { get; private set; }
    public BookingType Type { get; private set; }
    public BookingStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Booking Create(
        Guid shelterId,
        Guid bookerId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        int guests,
        BookingType type,
        DateTimeOffset now)
    {
        ValidateRange(startUtc, endUtc);
        ValidateGuests(guests);
        ValidateType(type);

        return new Booking
        {
            Id = Guid.NewGuid(),
            ShelterId = shelterId,
            BookerId = bookerId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Guests = guests,
            Type = type,
            Status = BookingStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Confirm(DateTimeOffset now)
    {
        if (Status == BookingStatus.Cancelled)
            throw new DomainValidationException("Cannot confirm a cancelled booking.");
        if (Status == BookingStatus.Confirmed) return;

        Status = BookingStatus.Confirmed;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == BookingStatus.Cancelled) return;

        Status = BookingStatus.Cancelled;
        UpdatedAt = now;
    }

    public void Reschedule(DateTimeOffset startUtc, DateTimeOffset endUtc, DateTimeOffset now)
    {
        if (Status == BookingStatus.Cancelled)
            throw new DomainValidationException("Cannot reschedule a cancelled booking.");

        ValidateRange(startUtc, endUtc);

        StartUtc = startUtc;
        EndUtc = endUtc;
        UpdatedAt = now;
    }

    public bool BookedBy(Guid userId) => BookerId == userId;

    private static void ValidateRange(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        if (endUtc <= startUtc)
            throw new DomainValidationException("Booking end must be after start.");
    }

    private static void ValidateGuests(int guests)
    {
        if (guests <= 0)
            throw new DomainValidationException("Guests must be positive.");
    }

    private static void ValidateType(BookingType type)
    {
        if (!Enum.IsDefined(type))
            throw new DomainValidationException("Unknown booking type.");
    }
}
