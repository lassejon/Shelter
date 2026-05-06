using App.Features.Reviews.SearchPicture;
using App.Features.Reviews.Shared;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Shelter.Api.Features.Reviews.SearchPicture;

public static class SearchReviewPictureEndpoint
{
    public static RouteGroupBuilder MapSearchReviewPicture(this RouteGroupBuilder group)
    {
        group.MapGet("/pictures", HandleAsync)
            .WithName("SearchReviewPicture")
            .WithSummary("List review pictures for the shelter (paginated)")
            .Produces<SearchReviewPictureResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        return group;
    }

    private static async Task<Ok<SearchReviewPictureResponse>> HandleAsync(
        Guid id,
        [AsParameters] PaginationParameters paging,
        SearchReviewPictureHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, paging, cancellationToken);
        return TypedResults.Ok(response);
    }
}
