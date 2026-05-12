using App.Common;
using App.Features.Shelters.Search;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Shelter.Api.Features.Shelters.Search;

public static class SearchShelterEndpoint
{
    public static RouteGroupBuilder MapSearchShelter(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("SearchShelter")
            .WithSummary("Search shelters by bounding box and filters")
            .Produces<CollectionResponse<SearchShelterResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous();

        return group;
    }

    private static async Task<Ok<CollectionResponse<SearchShelterResponse>>> HandleAsync(
        [AsParameters] SearchShelterRequest request,
        SearchShelterHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return TypedResults.Ok(response);
    }
}
