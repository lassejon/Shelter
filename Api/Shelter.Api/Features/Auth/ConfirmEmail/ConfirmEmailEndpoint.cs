using App.Features.Auth.ConfirmEmail;
using App.Features.Auth.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Shelter.Api.Features.Auth.ConfirmEmail;

public static class ConfirmEmailEndpoint
{
    public static RouteGroupBuilder MapConfirmEmail(this RouteGroupBuilder group)
    {
        group.MapPost("/confirm-email", HandleAsync)
            .WithName("ConfirmEmail")
            .WithSummary(
                "Confirm a user's email using the token sent during registration. On success " +
                "issues a JWT (auto-login) so the user lands signed in.")
            .AllowAnonymous();

        return group;
    }

    private static async Task<Results<Ok<AuthResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> HandleAsync(
        ConfirmEmailRequest request,
        ConfirmEmailHandler handler,
        CancellationToken cancellationToken)
    {
        var (response, failure, errors) = await handler.HandleAsync(request, cancellationToken);

        if (failure == ConfirmEmailFailure.UserNotFound)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "User not found",
                Status = StatusCodes.Status404NotFound,
                Detail = "No account matches the supplied id.",
            });
        }

        if (failure == ConfirmEmailFailure.InvalidToken)
        {
            var problem = new ProblemDetails
            {
                Title = "Invalid or expired confirmation token",
                Status = StatusCodes.Status400BadRequest,
                Detail = "The confirmation link is no longer valid. Request a new one and try again.",
            };
            problem.Extensions["errors"] = errors ?? Array.Empty<string>();
            return TypedResults.BadRequest(problem);
        }

        return TypedResults.Ok(response!);
    }
}
