using App.Common;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Bookings;
using Shelter.Domain.Common;

namespace App.Features.Bookings.SearchAvailability;

public sealed class SearchBookingAvailabilityHandler(
    IShelterDbContext db,
    IClock clock)
{
    public async Task<IReadOnlyList<BookingAvailabilityResponse>> HandleAsync(
        Guid shelterId,
        SearchBookingAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var shelterExists = await db.Shelters
            .AsNoTracking()
            .AnyAsync(s => s.Id == shelterId, cancellationToken);

        if (!shelterExists)
            throw new DomainNotFoundException($"Shelter {shelterId} was not found.");

        var from = NormalizeToDate(request.From ?? clock.UtcNow);
        var to = NormalizeToDate(request.To ?? from.AddDays(366));

        if (to <= from)
            throw new DomainValidationException("To must be after From.");

        var bookings = await db.Bookings
            .AsNoTracking()
            .Where(b => b.ShelterId == shelterId)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .Where(b => b.EndUtc >= from && b.StartUtc <= to)
            .OrderBy(b => b.StartUtc)
            .ToListAsync(cancellationToken);

        return bookings.Select(BookingAvailabilityResponse.FromDomain).ToList();
    }

    private static DateTimeOffset NormalizeToDate(DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, 0, 0, 0, TimeSpan.Zero);
}
