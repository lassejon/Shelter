using Shelter.Api.Features.Reviews.Delete;
using Shelter.Api.Features.Reviews.Get;
using Shelter.Api.Features.Reviews.Update;

namespace Shelter.Api.Features.Reviews;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reviews")
            .WithTags("Reviews");

        group.MapGetReview();
        group.MapUpdateReview();
        group.MapDeleteReview();

        return app;
    }
}
