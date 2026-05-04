using App.Features.Reviews.SearchByShelter;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Shelter.Api.Features.Reviews.SearchByShelter;

public static class SearchReviewByShelterEndpoint
{
    public static RouteGroupBuilder MapSearchReviewByShelter(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("SearchReviewByShelter")
            .WithSummary("List reviews for the shelter (paginated, with summary)")
            .Produces<SearchReviewByShelterResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        return group;
    }

    private static async Task<Ok<SearchReviewByShelterResponse>> HandleAsync(
        Guid id,
        [AsParameters] SearchReviewByShelterRequest request,
        SearchReviewByShelterHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return TypedResults.Ok(response);
    }
}
