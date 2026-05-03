using App.Common;
using App.Features.Bookings.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Bookings;
using Shelter.Domain.Common;

namespace App.Features.Bookings.Create;

public sealed class CreateBookingHandler(
    IShelterDbContext db,
    IClock clock,
    ILogger<CreateBookingHandler> logger)
{
    public async Task<BookingDetailResponse> HandleAsync(
        Guid shelterId,
        CreateBookingRequest request,
        Guid bookerId,
        CancellationToken cancellationToken)
    {
        var startUtc = NormalizeToDate(request.StartUtc);
        var endUtc = NormalizeToDate(request.EndUtc);

        var shelter = await db.Shelters
            .FirstOrDefaultAsync(s => s.Id == shelterId, cancellationToken)
            ?? throw new DomainNotFoundException($"Shelter {shelterId} was not found.");

        shelter.AssertCanBeBooked(request.Type, request.Guests);

        var overlapping = await db.Bookings
            .Where(b => b.ShelterId == shelterId)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .Where(b => b.StartUtc < endUtc && b.EndUtc > startUtc)
            .ToListAsync(cancellationToken);

        AssertNoConflicts(overlapping, request.Type, request.Guests, shelter.Capacity);

        var now = clock.UtcNow;
        var today = NormalizeToDate(now);

        var booking = Booking.Create(
            shelter.Id,
            bookerId,
            startUtc,
            endUtc,
            request.Guests,
            request.Type,
            today,
            now);

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {BookerId} created {Type} booking {BookingId} for shelter {ShelterId}",
            bookerId, booking.Type, booking.Id, shelter.Id);

        return BookingDetailResponse.FromDomain(booking);
    }

    private static DateTimeOffset NormalizeToDate(DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, 0, 0, 0, TimeSpan.Zero);

    private static void AssertNoConflicts(
        IReadOnlyList<Booking> overlapping,
        BookingType requestedType,
        int requestedGuests,
        int shelterCapacity)
    {
        if (overlapping.Count == 0) return;

        if (requestedType == BookingType.Exclusive)
            throw new DomainValidationException(
                "Cannot create exclusive booking — shelter has existing bookings during this period.");

        if (overlapping.Any(b => b.Type == BookingType.Exclusive))
            throw new DomainValidationException(
                "Cannot create booking — shelter is exclusively booked during this period.");

        var concurrentInclusiveGuests = overlapping
            .Where(b => b.Type == BookingType.Inclusive)
            .Sum(b => b.Guests);

        if (concurrentInclusiveGuests + requestedGuests > shelterCapacity)
            throw new DomainValidationException(
                $"Insufficient capacity — requested {requestedGuests} guests but only " +
                $"{shelterCapacity - concurrentInclusiveGuests} spots available during this period.");
    }
}
