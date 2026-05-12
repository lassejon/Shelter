using System.Security.Claims;
using App.Common;
using App.Features.Bookings.SearchByShelter;
using App.Features.Bookings.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Shelter.Api.Extensions;
using Shelter.Domain.Auth;

namespace Shelter.Api.Features.Bookings.SearchByShelter;

public static class SearchBookingByShelterEndpoint
{
    public static RouteGroupBuilder MapSearchBookingByShelter(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("SearchBookingByShelter")
            .WithSummary("List bookings on a shelter (owner view)")
            .Produces<CollectionResponse<BookingDetailResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AppPolicies.CanManageShelters);

        return group;
    }

    private static async Task<Ok<CollectionResponse<BookingDetailResponse>>> HandleAsync(
        Guid id,
        [AsParameters] SearchBookingByShelterRequest request,
        SearchBookingByShelterHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(id, request, user.GetUserId(), cancellationToken);
        return TypedResults.Ok(response);
    }
}
