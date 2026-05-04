using System.Security.Claims;
using App.Features.Reviews.Delete;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Reviews.Delete;

public static class DeleteReviewEndpoint
{
    public static RouteGroupBuilder MapDeleteReview(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", HandleAsync)
            .WithName("DeleteReview")
            .WithSummary("Delete one of your own reviews")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return group;
    }

    private static async Task<NoContent> HandleAsync(
        Guid id,
        DeleteReviewHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, user.GetUserId(), cancellationToken);
        return TypedResults.NoContent();
    }
}
