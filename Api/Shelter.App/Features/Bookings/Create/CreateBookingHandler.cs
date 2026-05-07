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

        var overlapping = await db.Bookings
            .Where(b => b.ShelterId == shelterId)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .Where(b => b.StartUtc < endUtc && b.EndUtc > startUtc)
            .Select(b => new BookingPeriod(b.StartUtc, b.EndUtc, b.Guests, b.Type))
            .ToListAsync(cancellationToken);

        shelter.AssertCanFit(overlapping, new BookingPeriod(startUtc, endUtc, request.Guests, request.Type));

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
}
