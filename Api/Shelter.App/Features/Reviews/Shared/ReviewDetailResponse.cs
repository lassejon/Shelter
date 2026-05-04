using App.Common;
using Shelter.Domain.Reviews;

namespace App.Features.Reviews.Shared;

public record ReviewDetailResponse(
    Guid Id,
    Guid ShelterId,
    Guid ReviewerId,
    string? ReviewerName,
    Rating Rating,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<PictureResponse> Pictures)
{
    public static ReviewDetailResponse FromDomain(Review review, IFileStorage storage) => new(
        review.Id,
        review.ShelterId,
        review.ReviewerId,
        review.Reviewer?.DisplayName,
        review.Rating,
        review.Comment,
        review.CreatedAt,
        review.UpdatedAt,
        review.Pictures
            .OrderBy(p => p.SortOrder)
            .Select(p => new PictureResponse(p.Id, storage.GetPublicUrl(p.Asset.BlobKey), p.Caption, p.SortOrder))
            .ToList());
}
