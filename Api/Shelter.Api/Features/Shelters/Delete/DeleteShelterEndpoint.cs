using System.Security.Claims;
using App.Features.Shelters.Delete;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;
using Shelter.Domain.Auth;

namespace Shelter.Api.Features.Shelters.Delete;

public static class DeleteShelterEndpoint
{
    public static RouteGroupBuilder MapDeleteShelter(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", HandleAsync)
            .WithName("DeleteShelter")
            .WithSummary("Delete a shelter")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AppPolicies.CanManageShelters);

        return group;
    }

    private static async Task<NoContent> HandleAsync(
        Guid id,
        DeleteShelterHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, user.GetUserId(), cancellationToken);
        return TypedResults.NoContent();
    }
}
