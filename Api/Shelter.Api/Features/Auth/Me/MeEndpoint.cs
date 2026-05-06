using System.Security.Claims;
using App.Features.Auth.Me;
using App.Features.Auth.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Auth.Me;

public static class MeEndpoint
{
    public static RouteGroupBuilder MapMe(this RouteGroupBuilder group)
    {
        group.MapGet("/me", HandleAsync)
            .WithName("Me")
            .WithSummary("Return a fresh AuthResponse (with a new JWT) for the current user.")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return group;
    }

    private static async Task<Ok<AuthResponse>> HandleAsync(
        MeHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(user.GetUserId(), cancellationToken);
        return TypedResults.Ok(response);
    }
}
