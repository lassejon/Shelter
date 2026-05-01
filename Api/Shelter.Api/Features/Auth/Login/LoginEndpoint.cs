using App.Features.Auth.Login;
using App.Features.Auth.Shared;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Shelter.Api.Features.Auth.Login;

public static class LoginEndpoint
{
    public static RouteGroupBuilder MapLogin(this RouteGroupBuilder group)
    {
        group.MapPost("/login", HandleAsync)
            .WithName("Login")
            .WithSummary("Log in with email and password; returns a JWT access token.")
            .AllowAnonymous();

        return group;
    }

    private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> HandleAsync(
        LoginRequest request,
        LoginHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(request, cancellationToken);

        return response is null
            ? TypedResults.Unauthorized()
            : TypedResults.Ok(response);
    }
}
