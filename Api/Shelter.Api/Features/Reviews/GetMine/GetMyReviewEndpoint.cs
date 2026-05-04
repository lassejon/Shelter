using System.Security.Claims;
using App.Features.Reviews.GetMine;
using App.Features.Reviews.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Reviews.GetMine;

public static class GetMyReviewEndpoint
{
    public static RouteGroupBuilder MapGetMyReview(this RouteGroupBuilder group)
    {
        group.MapGet("/mine", HandleAsync)
            .WithName("GetMyReview")
            .WithSummary("Get the current user's review for the shelter")
            .Produces<ReviewDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return group;
    }

    private static async Task<Ok<ReviewDetailResponse>> HandleAsync(
        Guid id,
        GetMyReviewHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, user.GetUserId(), cancellationToken);
        return TypedResults.Ok(response);
    }
}
