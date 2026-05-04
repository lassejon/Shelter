using App.Common;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Reviews.Delete;

public sealed class DeleteReviewHandler(
    IShelterDbContext db,
    IFileStorage storage,
    AssetOrphanRecovery orphanRecovery,
    ILogger<DeleteReviewHandler> logger)
{
    public async Task HandleAsync(
        Guid reviewId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var review = await db.Reviews
            .Include(r => r.Pictures)
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken)
            ?? throw new DomainNotFoundException($"Review {reviewId} was not found.");

        if (!review.WrittenBy(userId))
            throw new DomainAuthorizationException("You can only delete your own reviews.");

        var freedAssetIds = review.Pictures.Select(p => p.AssetId).ToList();
        var deletedPictureIds = review.Pictures.Select(p => p.Id).ToList();

        var blobsToDelete = await orphanRecovery.QueueOrphansFromDeletedPicturesAsync(
            freedAssetIds,
            deletedReviewPictureIds: deletedPictureIds,
            cancellationToken: cancellationToken);

        db.Reviews.Remove(review);
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

        logger.LogInformation("User {UserId} deleted review {ReviewId}", userId, review.Id);
    }
}
