using App.Features.Auth.ResendConfirmationEmail;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Shelter.Api.Features.Auth.ResendConfirmationEmail;

public static class ResendConfirmationEmailEndpoint
{
    public static RouteGroupBuilder MapResendConfirmationEmail(this RouteGroupBuilder group)
    {
        group.MapPost("/resend-confirmation", HandleAsync)
            .WithName("ResendConfirmationEmail")
            .WithSummary(
                "Re-send the email confirmation link. Always returns 204 regardless of whether " +
                "the email exists or is already confirmed (the API does not leak account state).")
            .AllowAnonymous();

        return group;
    }

    private static async Task<NoContent> HandleAsync(
        ResendConfirmationEmailRequest request,
        ResendConfirmationEmailHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request, cancellationToken);
        return TypedResults.NoContent();
    }
}
