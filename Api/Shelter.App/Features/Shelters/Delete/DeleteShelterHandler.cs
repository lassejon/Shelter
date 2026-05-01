using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Shelters.Delete;

public sealed class DeleteShelterHandler(
    IShelterDbContext db,
    ILogger<DeleteShelterHandler> logger)
{
    public async Task HandleAsync(
        Guid shelterId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var shelter = await db.Shelters
            .FirstOrDefaultAsync(s => s.Id == shelterId, cancellationToken)
            ?? throw new DomainNotFoundException($"Shelter {shelterId} was not found.");

        if (!shelter.OwnedBy(userId))
            throw new DomainAuthorizationException("You can only delete your own shelters.");

        db.Shelters.Remove(shelter);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted shelter {ShelterId} for owner {OwnerId}", shelter.Id, shelter.OwnerId);
    }
}
