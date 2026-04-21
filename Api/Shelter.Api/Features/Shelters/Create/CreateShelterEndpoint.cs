using App.Common;
using App.Features.Shelters.Create;
using App.Features.Shelters.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Shelter.Api.Features.Shelters.Create;

public static class CreateShelterEndpoint
{
    // Replace with HttpContext.User.GetUserId() once JWT auth is wired.
    private static readonly Guid DevOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static RouteGroupBuilder MapCreateShelter(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("CreateShelter")
            .WithSummary("Create a new shelter")
            .Accepts<CreateShelterRequest>("multipart/form-data")
            .Produces<ShelterDetailResponse>(StatusCodes.Status201Created)
            .DisableAntiforgery();

        return group;
    }

    private static async Task<Created<ShelterDetailResponse>> HandleAsync(
        [FromForm] CreateShelterRequest request,
        IFormFileCollection pictures,
        CreateShelterHandler handler,
        CancellationToken cancellationToken)
    {
        var uploads = pictures
            .Select(p => new FileUpload(p.OpenReadStream(), p.ContentType, p.FileName))
            .ToList();

        var response = await handler.HandleAsync(request, DevOwnerId, uploads, cancellationToken);

        return TypedResults.Created($"/api/shelters/{response.Id}", response);
    }
}
