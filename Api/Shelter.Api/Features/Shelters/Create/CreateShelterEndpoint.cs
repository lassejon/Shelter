using System.Security.Claims;
using App.Common;
using App.Features.Shelters.Create;
using App.Features.Shelters.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shelter.Api.Extensions;
using Shelter.Domain.Auth;

namespace Shelter.Api.Features.Shelters.Create;

public static class CreateShelterEndpoint
{
    public static RouteGroupBuilder MapCreateShelter(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("CreateShelter")
            .WithSummary("Create a new shelter")
            .Accepts<CreateShelterRequest>("multipart/form-data")
            .Produces<ShelterDetailResponse>(StatusCodes.Status201Created)
            .RequireAuthorization(AppPolicies.CanManageShelters)
            .DisableAntiforgery();

        return group;
    }

    private static async Task<Created<ShelterDetailResponse>> HandleAsync(
        [FromForm] CreateShelterRequest request,
        IFormFileCollection pictures,
        CreateShelterHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var uploads = pictures
            .Select(p => new FileUpload(p.OpenReadStream(), p.ContentType, p.FileName))
            .ToList();

        var response = await handler.HandleAsync(request, user.GetUserId(), uploads, cancellationToken);

        return TypedResults.Created($"/api/shelters/{response.Id}", response);
    }
}
