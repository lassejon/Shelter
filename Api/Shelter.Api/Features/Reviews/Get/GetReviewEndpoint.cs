using App.Features.Reviews.Get;
using App.Features.Reviews.Shared;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Shelter.Api.Features.Reviews.Get;

public static class GetReviewEndpoint
{
    public static RouteGroupBuilder MapGetReview(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", HandleAsync)
            .WithName("GetReview")
            .WithSummary("Get a review by id")
            .Produces<ReviewDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        return group;
    }

    private static async Task<Ok<ReviewDetailResponse>> HandleAsync(
        Guid id,
        GetReviewHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, cancellationToken);
        return TypedResults.Ok(response);
    }
}
