using App.Features.Shelters.Get;
using App.Features.Shelters.Shared;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Shelter.Api.Features.Shelters.Get;

public static class GetShelterEndpoint
{
    public static RouteGroupBuilder MapGetShelter(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", HandleAsync)
            .WithName("GetShelter")
            .WithSummary("Get a shelter by id")
            .Produces<ShelterDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        return group;
    }

    private static async Task<Ok<ShelterDetailResponse>> HandleAsync(
        Guid id,
        GetShelterHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, cancellationToken);
        return TypedResults.Ok(response);
    }
}
