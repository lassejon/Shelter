using Shelter.Domain.Reviews;

namespace App.Features.Reviews.Shared;

public record ReviewSummary(double AverageRating, int TotalCount)
{
    public static ReviewSummary Empty { get; } = new(0d, 0);

    public static ReviewSummary FromReviews(IEnumerable<Review> reviews)
    {
        var list = reviews as IReadOnlyCollection<Review> ?? reviews.ToList();
        if (list.Count == 0) return Empty;
        return new ReviewSummary(Math.Round(list.Average(r => (int)r.Rating), 2), list.Count);
    }
}
