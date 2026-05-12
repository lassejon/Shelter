using App.Common;
using App.Features.Bookings.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Bookings;

namespace App.Features.Bookings.SearchByBooker;

public sealed class SearchBookingByBookerHandler(IShelterDbContext db)
{
    public async Task<CollectionResponse<BookingDetailResponse>> HandleAsync(
        SearchBookingByBookerRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Booker)
            .Where(b => b.BookerId == userId);

        if (!request.IncludeHistory)
            query = query.Where(b => b.Status != BookingStatus.Cancelled);

        var bookings = await query
            .OrderBy(b => b.StartUtc)
            .ToListAsync(cancellationToken);

        var items = bookings.Select(BookingDetailResponse.FromDomain).ToList();
        return new CollectionResponse<BookingDetailResponse>(items);
    }
}
