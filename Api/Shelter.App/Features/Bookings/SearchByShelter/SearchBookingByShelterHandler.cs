using App.Features.Bookings.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Bookings.SearchByShelter;

public sealed class SearchBookingByShelterHandler(IShelterDbContext db)
{
    public async Task<IReadOnlyList<BookingDetailResponse>> HandleAsync(
        Guid shelterId,
        SearchBookingByShelterRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var shelter = await db.Shelters
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shelterId, cancellationToken)
            ?? throw new DomainNotFoundException($"Shelter {shelterId} was not found.");

        if (!shelter.OwnedBy(userId))
            throw new DomainAuthorizationException("You can only view bookings for your own shelters.");

        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Booker)
            .Where(b => b.ShelterId == shelterId);

        if (request.From.HasValue)
            query = query.Where(b => b.EndUtc >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(b => b.StartUtc <= request.To.Value);

        var bookings = await query
            .OrderBy(b => b.StartUtc)
            .ToListAsync(cancellationToken);

        return bookings.Select(BookingDetailResponse.FromDomain).ToList();
    }
}
