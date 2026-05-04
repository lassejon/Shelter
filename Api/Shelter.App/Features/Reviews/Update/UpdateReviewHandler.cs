using App.Common;
using App.Features.Reviews.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Assets;
using Shelter.Domain.Common;

namespace App.Features.Reviews.Update;

public sealed class UpdateReviewHandler(
    IShelterDbContext db,
    IClock clock,
    IFileStorage storage,
    AssetOrphanRecovery orphanRecovery,
    ILogger<UpdateReviewHandler> logger)
{
    public async Task<ReviewDetailResponse> HandleAsync(
        Guid reviewId,
        UpdateReviewRequest request,
        Guid userId,
        IReadOnlyList<FileUpload> newPictures,
        CancellationToken cancellationToken)
    {
        var review = await db.Reviews
            .Include(r => r.Pictures)
                .ThenInclude(p => p.Asset)
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken)
            ?? throw new DomainNotFoundException($"Review {reviewId} was not found.");

        if (!review.WrittenBy(userId))
            throw new DomainAuthorizationException("You can only update your own reviews.");

        var now = clock.UtcNow;

        var hasScalarChange = request.Rating.HasValue || request.Comment is not null;
        if (hasScalarChange)
        {
            review.Edit(
                request.Rating  ?? review.Rating,
                request.Comment ?? review.Comment,
                now);
        }

        var freedAssetIds = new List<Guid>();
        var deletedPictureIds = new List<Guid>();

        if (request.PictureIdsToDelete is { Count: > 0 })
        {
            foreach (var pictureId in request.PictureIdsToDelete)
            {
                var picture = review.Pictures.FirstOrDefault(p => p.Id == pictureId);
                if (picture is null) continue;
                freedAssetIds.Add(picture.AssetId);
                deletedPictureIds.Add(picture.Id);
                review.RemovePicture(pictureId, now);
            }
        }

        foreach (var picture in newPictures)
        {
            var asset = await UploadAsync(userId, review.Id, picture, now, cancellationToken);
            db.Assets.Add(asset);
            review.AddPicture(asset.Id, caption: null, now);
        }

        var blobsToDelete = await orphanRecovery.QueueOrphansFromDeletedPicturesAsync(
            freedAssetIds,
            deletedReviewPictureIds: deletedPictureIds,
            cancellationToken: cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await DeleteBlobsBestEffortAsync(blobsToDelete, cancellationToken);

        logger.LogInformation("User {UserId} updated review {ReviewId}", userId, review.Id);

        return ReviewDetailResponse.FromDomain(review, storage);
    }

    private async Task<Asset> UploadAsync(
        Guid uploaderId,
        Guid reviewId,
        FileUpload picture,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var blobKey = $"reviews/{reviewId}/{Guid.NewGuid():N}";
        await storage.UploadAsync(blobKey, picture.Content, picture.ContentType, cancellationToken);
        return Asset.Create(uploaderId, blobKey, picture.ContentType, now);
    }

    private async Task DeleteBlobsBestEffortAsync(
        IReadOnlyList<string> blobKeys,
        CancellationToken cancellationToken)
    {
        foreach (var blobKey in blobKeys)
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
    }
}
