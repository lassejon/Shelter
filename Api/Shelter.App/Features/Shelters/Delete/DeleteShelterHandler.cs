using App.Common;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Shelters.Delete;

public sealed class DeleteShelterHandler(
    IShelterDbContext db,
    IFileStorage storage,
    AssetOrphanRecovery orphanRecovery,
    ILogger<DeleteShelterHandler> logger)
{
    public async Task HandleAsync(
        Guid shelterId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var shelter = await db.Shelters
            .Include(s => s.Pictures)
            .FirstOrDefaultAsync(s => s.Id == shelterId, cancellationToken)
            ?? throw new DomainNotFoundException($"Shelter {shelterId} was not found.");

        if (!shelter.OwnedBy(userId))
            throw new DomainAuthorizationException("You can only delete your own shelters.");

        var freedAssetIds = shelter.Pictures.Select(p => p.AssetId).ToList();
        var deletedPictureIds = shelter.Pictures.Select(p => p.Id).ToList();

        var blobsToDelete = await orphanRecovery.QueueOrphansFromDeletedPicturesAsync(
            freedAssetIds,
            deletedShelterPictureIds: deletedPictureIds,
            cancellationToken: cancellationToken);

        db.Shelters.Remove(shelter);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var blobKey in blobsToDelete)
        {
            try
            {
                await storage.DeleteAsync(blobKey, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to delete orphaned blob {BlobKey}; sweeper will retry",
                    blobKey);
            }
        }

        logger.LogInformation("Deleted shelter {ShelterId} for owner {OwnerId}", shelter.Id, shelter.OwnerId);
    }
}
