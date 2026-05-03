using System.Security.Claims;
using App.Features.Bookings.Create;
using App.Features.Bookings.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Bookings.Create;

public static class CreateBookingEndpoint
{
    public static RouteGroupBuilder MapCreateBooking(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("CreateBooking")
            .WithSummary("Create a booking for the shelter")
            .Accepts<CreateBookingRequest>("application/json")
            .Produces<BookingDetailResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return group;
    }

    private static async Task<Created<BookingDetailResponse>> HandleAsync(
        Guid id,
        CreateBookingRequest request,
        CreateBookingHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, request, user.GetUserId(), cancellationToken);
        return TypedResults.Created($"/api/bookings/{response.Id}", response);
    }
}
