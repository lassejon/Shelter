using System.Security.Claims;
using App.Features.Auth.Shared;
using App.Features.Auth.UpgradeToOwner;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Auth.UpgradeToOwner;

public static class UpgradeToOwnerEndpoint
{
    public static RouteGroupBuilder MapUpgradeToOwner(this RouteGroupBuilder group)
    {
        group.MapPost("/upgrade-to-owner", HandleAsync)
            .WithName("UpgradeToOwner")
            .WithSummary("Add the ShelterOwner role to the current user; returns a fresh JWT with the new role.")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return group;
    }

    private static async Task<Ok<AuthResponse>> HandleAsync(
        UpgradeToOwnerHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(user.GetUserId(), cancellationToken);
        return TypedResults.Ok(response);
    }
}
