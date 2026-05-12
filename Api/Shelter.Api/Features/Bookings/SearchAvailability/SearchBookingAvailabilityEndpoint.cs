using App.Common;
using App.Features.Bookings.SearchAvailability;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Shelter.Api.Features.Bookings.SearchAvailability;

public static class SearchBookingAvailabilityEndpoint
{
    public static RouteGroupBuilder MapSearchBookingAvailability(this RouteGroupBuilder group)
    {
        group.MapGet("/availability", HandleAsync)
            .WithName("SearchBookingAvailability")
            .WithSummary("List booking occupancy for a shelter without exposing booker details.")
            .Produces<CollectionResponse<BookingAvailabilityResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return group;
    }

    private static async Task<Ok<CollectionResponse<BookingAvailabilityResponse>>> HandleAsync(
        Guid id,
        [AsParameters] SearchBookingAvailabilityRequest request,
        SearchBookingAvailabilityHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, request, cancellationToken);
        return TypedResults.Ok(response);
    }
}
