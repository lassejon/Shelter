using Shelter.Api.Features.Bookings.Cancel;
using Shelter.Api.Features.Bookings.Get;
using Shelter.Api.Features.Bookings.SearchByBooker;

namespace Shelter.Api.Features.Bookings;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings")
            .WithTags("Bookings");

        group.MapGetBooking();
        group.MapSearchBookingByBooker();
        group.MapCancelBooking();

        return app;
    }
}
