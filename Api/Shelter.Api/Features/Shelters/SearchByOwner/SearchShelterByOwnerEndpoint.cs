using System.Security.Claims;
using App.Common;
using App.Features.Shelters.SearchByOwner;
using App.Features.Shelters.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Shelters.SearchByOwner;

public static class SearchShelterByOwnerEndpoint
{
    public static RouteGroupBuilder MapSearchShelterByOwner(this RouteGroupBuilder group)
    {
        group.MapGet("/mine", HandleAsync)
            .WithName("SearchShelterByOwner")
            .WithSummary("List every shelter you own (active and deactivated)")
            .Produces<CollectionResponse<ShelterDetailResponse>>(StatusCodes.Status200OK)
            .RequireAuthorization();

        return group;
    }

    private static async Task<Ok<CollectionResponse<ShelterDetailResponse>>> HandleAsync(
        SearchShelterByOwnerHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(user.GetUserId(), cancellationToken);
        return TypedResults.Ok(response);
    }
}
