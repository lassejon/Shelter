using System.Security.Claims;
using App.Features.Bookings.Cancel;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Bookings.Cancel;

public static class CancelBookingEndpoint
{
    public static RouteGroupBuilder MapCancelBooking(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", HandleAsync)
            .WithName("CancelBooking")
            .WithSummary("Cancel one of your own bookings")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return group;
    }

    private static async Task<NoContent> HandleAsync(
        Guid id,
        CancelBookingHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, user.GetUserId(), cancellationToken);
        return TypedResults.NoContent();
    }
}
