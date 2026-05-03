using System.Security.Claims;
using App.Features.Bookings.Get;
using App.Features.Bookings.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Bookings.Get;

public static class GetBookingEndpoint
{
    public static RouteGroupBuilder MapGetBooking(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", HandleAsync)
            .WithName("GetBooking")
            .WithSummary("Get one of your own bookings by id")
            .Produces<BookingDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return group;
    }

    private static async Task<Ok<BookingDetailResponse>> HandleAsync(
        Guid id,
        GetBookingHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, user.GetUserId(), cancellationToken);
        return TypedResults.Ok(response);
    }
}
