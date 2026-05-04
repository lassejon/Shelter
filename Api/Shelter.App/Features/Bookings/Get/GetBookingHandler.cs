using App.Features.Bookings.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Bookings.Get;

public sealed class GetBookingHandler(IShelterDbContext db)
{
    public async Task<BookingDetailResponse> HandleAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var booking = await db.Bookings
            .Include(b => b.Booker)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new DomainNotFoundException($"Booking {id} was not found.");

        if (!booking.BookedBy(userId))
            throw new DomainAuthorizationException("You can only view your own bookings.");

        return BookingDetailResponse.FromDomain(booking);
    }
}
