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

        if (!booking.BookedBy(userId))
            throw new DomainAuthorizationException("You can only cancel your own bookings.");

        booking.Cancel(clock.UtcNow);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} cancelled booking {BookingId}", userId, booking.Id);
    }
}
