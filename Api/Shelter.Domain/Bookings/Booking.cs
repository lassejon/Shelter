using Shelter.Domain.Common;
using Shelter.Domain.Shelters;
using Shelter.Domain.Users;

namespace Shelter.Domain.Bookings;

public class Booking
{
    private Booking() { }

    public Guid Id { get; private set; }
    public Guid ShelterId { get; private set; }
    public Guid BookerId { get; private set; }
    public User? Booker { get; private set; }

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
        BookingApprovalMode shelterApprovalMode,
        DateTimeOffset today,
        DateTimeOffset now)
    {
        ValidateNotInPast(startUtc, today);
        ValidateRange(startUtc, endUtc);
        ValidateMaxNights(startUtc, endUtc);
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
            Status = shelterApprovalMode == BookingApprovalMode.RequiresApproval
                ? BookingStatus.Pending
                : BookingStatus.Confirmed,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Confirm(DateTimeOffset now)
    {
        if (Status == BookingStatus.Cancelled)
            throw new DomainValidationException("Cannot confirm a cancelled booking.");
        if (Status == BookingStatus.Confirmed) return;
        if (now >= StartUtc)
            throw new DomainValidationException("Cannot confirm a booking that has started or is in the past.");

        Status = BookingStatus.Confirmed;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == BookingStatus.Cancelled)
            throw new DomainValidationException("Booking is already cancelled.");
        if (now >= StartUtc)
            throw new DomainValidationException("Cannot cancel a booking that has started or is in the past.");

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
            throw new DomainValidationException("Booking end must be after start (at least 1 night).");
    }

    private static void ValidateNotInPast(DateTimeOffset startUtc, DateTimeOffset today)
    {
        if (startUtc < today)
            throw new DomainValidationException("Booking start must be today or in the future.");
    }

    private static void ValidateMaxNights(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        var nights = (endUtc - startUtc).Days;
        if (nights > 365)
            throw new DomainValidationException("Booking duration cannot exceed 365 nights.");
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
