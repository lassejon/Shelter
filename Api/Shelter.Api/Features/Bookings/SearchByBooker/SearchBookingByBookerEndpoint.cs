using System.Security.Claims;
using App.Common;
using App.Features.Bookings.SearchByBooker;
using App.Features.Bookings.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Bookings.SearchByBooker;

public static class SearchBookingByBookerEndpoint
{
    public static RouteGroupBuilder MapSearchBookingByBooker(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("SearchBookingByBooker")
            .WithSummary("List your own bookings (booker view)")
            .Produces<CollectionResponse<BookingDetailResponse>>(StatusCodes.Status200OK)
            .RequireAuthorization();

        return group;
    }

    private static async Task<Ok<CollectionResponse<BookingDetailResponse>>> HandleAsync(
        [AsParameters] SearchBookingByBookerRequest request,
        SearchBookingByBookerHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(request, user.GetUserId(), cancellationToken);
        return TypedResults.Ok(response);
    }
}
