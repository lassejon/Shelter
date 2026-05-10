using App.Common;
using App.Features.Bookings.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Bookings.Approve;

public sealed class ApproveBookingHandler(
    IShelterDbContext db,
    IClock clock,
    ILogger<ApproveBookingHandler> logger)
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

        var shelter = await db.Shelters
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == booking.ShelterId, cancellationToken)
            ?? throw new DomainNotFoundException($"Shelter {booking.ShelterId} was not found.");

        if (!shelter.OwnedBy(userId))
            throw new DomainAuthorizationException("You can only approve bookings on your own shelters.");

        booking.Confirm(clock.UtcNow);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Owner {UserId} approved booking {BookingId} on shelter {ShelterId}",
            userId, booking.Id, shelter.Id);

        return BookingDetailResponse.FromDomain(booking);
    }
}
