using App.Features.Auth.Login;
using App.Features.Auth.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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

    private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult, ProblemHttpResult>> HandleAsync(
        LoginRequest request,
        LoginHandler handler,
        CancellationToken cancellationToken)
    {
        var (response, failure) = await handler.HandleAsync(request, cancellationToken);

        if (failure == LoginFailure.InvalidCredentials)
        {
            return TypedResults.Unauthorized();
        }

        if (failure == LoginFailure.EmailNotConfirmed)
        {
            // 403 Forbidden: the credentials are valid, but the account isn't activated. The
            // `code` extension lets the UI distinguish this case from a generic forbid and
            // surface a "resend confirmation" affordance.
            return TypedResults.Problem(new ProblemDetails
            {
                Title = "Email not confirmed",
                Status = StatusCodes.Status403Forbidden,
                Detail =
                    "Please confirm your email address before logging in. " +
                    "Check your inbox for the confirmation link, or request a new one.",
                Extensions = { ["code"] = "email_not_confirmed" },
            });
        }

        return TypedResults.Ok(response!);
    }
}
