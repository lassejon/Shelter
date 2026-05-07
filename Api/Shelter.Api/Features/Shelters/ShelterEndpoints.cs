using Shelter.Api.Features.Bookings.Create;
using Shelter.Api.Features.Bookings.SearchAvailability;
using Shelter.Api.Features.Bookings.SearchByShelter;
using Shelter.Api.Features.Reviews.Create;
using Shelter.Api.Features.Reviews.GetMine;
using Shelter.Api.Features.Reviews.SearchByShelter;
using Shelter.Api.Features.Reviews.SearchPicture;
using Shelter.Api.Features.Shelters.Create;
using Shelter.Api.Features.Shelters.Delete;
using Shelter.Api.Features.Shelters.Get;
using Shelter.Api.Features.Shelters.Search;
using Shelter.Api.Features.Shelters.Update;

namespace Shelter.Api.Features.Shelters;

public static class ShelterEndpoints
{
    public static IEndpointRouteBuilder MapShelterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shelters")
            .WithTags("Shelters");

        group.MapCreateShelter();
        group.MapGetShelter();
        group.MapSearchShelter();
        group.MapUpdateShelter();
        group.MapDeleteShelter();

        // Nested booking sub-resources: create / list bookings *for a specific shelter*.
        // Slice files live under Features/Bookings/, only the URL mounting is nested here.
        var bookingsUnderShelter = group.MapGroup("/{id:guid}/bookings");
        bookingsUnderShelter.MapCreateBooking();
        bookingsUnderShelter.MapSearchBookingAvailability();
        bookingsUnderShelter.MapSearchBookingByShelter();

        // Nested review sub-resources: create / list reviews *of a specific shelter*,
        // plus the current user's review and the gallery of all review pictures.
        var reviewsUnderShelter = group.MapGroup("/{id:guid}/reviews");
        reviewsUnderShelter.MapCreateReview();
        reviewsUnderShelter.MapSearchReviewByShelter();
        reviewsUnderShelter.MapGetMyReview();
        reviewsUnderShelter.MapSearchReviewPicture();

        return app;
    }
}
