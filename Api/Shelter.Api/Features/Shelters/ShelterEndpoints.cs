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

        return app;
    }
}
