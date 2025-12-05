using Shelter.Domain.Shelters;

namespace Shelter.Application.Responses;

public record ReviewSummary(
    double AverageRating,
    int TotalCount)
{
    public static ReviewSummary FromReviews(ICollection<Review> reviews)
    {
        if (reviews == null || reviews.Count == 0)
        {
            return new ReviewSummary(0, 0);
        }

        var averageRating = reviews.Average(r => (int)r.Rating);
        return new ReviewSummary(Math.Round(averageRating, 2), reviews.Count);
    }
}
