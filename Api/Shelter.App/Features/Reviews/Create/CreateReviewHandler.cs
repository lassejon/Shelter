using App.Common;
using App.Features.Reviews.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Assets;
using Shelter.Domain.Common;
using Shelter.Domain.Reviews;

namespace App.Features.Reviews.Create;

public sealed class CreateReviewHandler(
    IShelterDbContext db,
    IClock clock,
    IFileStorage storage,
    ILogger<CreateReviewHandler> logger)
{
    public async Task<ReviewDetailResponse> HandleAsync(
        Guid shelterId,
        CreateReviewRequest request,
        Guid reviewerId,
        IReadOnlyList<FileUpload> pictures,
        CancellationToken cancellationToken)
    {
        var shelterExists = await db.Shelters
            .AnyAsync(s => s.Id == shelterId, cancellationToken);
        if (!shelterExists)
            throw new DomainNotFoundException($"Shelter {shelterId} was not found.");

        var alreadyReviewed = await db.Reviews
            .AnyAsync(r => r.ShelterId == shelterId && r.ReviewerId == reviewerId, cancellationToken);
        if (alreadyReviewed)
            throw new DomainValidationException(
                "You have already reviewed this shelter. Use update instead.");

        var now = clock.UtcNow;
        var review = Review.Create(shelterId, reviewerId, request.Rating, request.Comment, now);

        foreach (var picture in pictures)
        {
            var asset = await UploadAsync(reviewerId, review.Id, picture, now, cancellationToken);
            db.Assets.Add(asset);
            review.AddPicture(asset.Id, caption: null, now);
        }

        db.Reviews.Add(review);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {ReviewerId} created review {ReviewId} for shelter {ShelterId} with {PictureCount} pictures",
            reviewerId, review.Id, shelterId, review.Pictures.Count);

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
}
