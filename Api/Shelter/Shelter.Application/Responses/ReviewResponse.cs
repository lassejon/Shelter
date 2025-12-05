using Shelter.Domain.Shelters;

namespace Shelter.Application.Responses;

public record ReviewResponse(
    Guid Id,
    Guid ShelterId,
    Guid ReviewerId,
    string ReviewerName,
    Rating Rating,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<PictureResponse> Pictures)
{
    public static ReviewResponse FromDomain(Review review) => new(
        review.Id,
        review.ShelterId,
        review.ReviewerId,
        review.Reviewer.FirstName,
        review.Rating,
        review.Comment,
        review.CreatedAt,
        review.UpdatedAt,
        review.Pictures
            .OrderBy(p => p.SortOrder)
            .Select(PictureResponse.FromDomain)
            .ToList());
}
