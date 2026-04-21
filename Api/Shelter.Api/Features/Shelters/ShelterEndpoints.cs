using Shelter.Api.Features.Shelters.Create;

namespace Shelter.Api.Features.Shelters;

public static class ShelterEndpoints
{
    public static IEndpointRouteBuilder MapShelterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shelters")
            .WithTags("Shelters");

        group.MapCreateShelter();

        return app;
    }
}
