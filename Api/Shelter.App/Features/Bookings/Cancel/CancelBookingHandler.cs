using App.Common;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Bookings.Cancel;

public sealed class CancelBookingHandler(
    IShelterDbContext db,
    IClock clock,
    ILogger<CancelBookingHandler> logger)
{
    public async Task HandleAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new DomainNotFoundException($"Booking {id} was not found.");

        // Either the booker (cancelling their own trip) or the shelter owner
        // (rejecting / cancelling a booking on their shelter) is allowed.
        var isBooker = booking.BookedBy(userId);
        var isOwner = !isBooker && await db.Shelters
            .AsNoTracking()
            .AnyAsync(s => s.Id == booking.ShelterId && s.OwnerId == userId, cancellationToken);

        if (!isBooker && !isOwner)
            throw new DomainAuthorizationException(
                "You can only cancel bookings you made or that are on shelters you own.");

        booking.Cancel(clock.UtcNow);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} cancelled booking {BookingId} (as {Role})",
            userId, booking.Id, isBooker ? "booker" : "owner");
    }
}
