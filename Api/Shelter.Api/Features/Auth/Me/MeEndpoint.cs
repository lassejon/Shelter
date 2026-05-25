using System.Security.Claims;
using App.Features.Auth.Me;
using App.Features.Auth.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Auth.Me;

// Originally intended as an ad-hoc session-renewal hook (re-issues a JWT on every call) to defer
// implementing real refresh tokens. With the current FE cache config it only fires once at app
// boot, so the effective role is boot-time identity reconciliation (stale localStorage vs. fresh
// roles/profile, detect account-deleted). Session extension belongs to refresh tokens — TODO.
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
