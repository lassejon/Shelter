using App.Features.Auth.Register;
using App.Features.Auth.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Shelter.Api.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static RouteGroupBuilder MapRegister(this RouteGroupBuilder group)
    {
        group.MapPost("/register", HandleAsync)
            .WithName("Register")
            .WithSummary("Register a new user and return a JWT access token (auto-login).")
            .AllowAnonymous();

        return group;
    }

    private static async Task<Results<Ok<AuthResponse>, Conflict<ProblemDetails>>> HandleAsync(
        RegisterRequest request,
        RegisterHandler handler,
        CancellationToken cancellationToken)
    {
        var (response, failure) = await handler.HandleAsync(request, cancellationToken);

        if (failure == RegisterFailure.EmailAlreadyExists)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Email already registered",
                Status = StatusCodes.Status409Conflict,
                Detail = $"An account with email '{request.Email}' already exists.",
            });
        }

        return TypedResults.Ok(response!);
    }
}
