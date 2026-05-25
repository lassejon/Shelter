namespace Shelter.Api.Features.Auth.Logout;

public static class LogoutEndpoint
{
    // Redundant endpoint for future proofing when refresh tokens are introduced and should be invalidated when logging out
    
    public static RouteGroupBuilder MapLogout(this RouteGroupBuilder group)
    {
        group.MapPost("/logout", () => TypedResults.NoContent())
            .WithName("Logout")
            .WithSummary("Logout (no-op for stateless JWT; requires a valid token for audit).")
            .RequireAuthorization();

        return group;
    }
}
