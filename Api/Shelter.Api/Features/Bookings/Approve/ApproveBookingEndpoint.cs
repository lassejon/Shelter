using System.Security.Claims;
using App.Features.Bookings.Approve;
using App.Features.Bookings.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Bookings.Approve;

public static class ApproveBookingEndpoint
{
    public static RouteGroupBuilder MapApproveBooking(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/approve", HandleAsync)
            .WithName("ApproveBooking")
            .WithSummary("Approve a pending booking on a shelter you own")
            .Produces<BookingDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return group;
    }

    private static async Task<Ok<BookingDetailResponse>> HandleAsync(
        Guid id,
        ApproveBookingHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, user.GetUserId(), cancellationToken);
        return TypedResults.Ok(response);
    }
}
