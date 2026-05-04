using App.Features.Reviews.Shared;

namespace App.Features.Reviews.SearchByShelter;

public record SearchReviewByShelterResponse(
    List<ReviewDetailResponse> Reviews,
    ReviewSummary Summary,
    Pagination Pagination);
