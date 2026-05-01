using System.Security.Claims;
using App.Common;
using App.Features.Shelters.Shared;
using App.Features.Shelters.Update;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shelter.Api.Extensions;
using Shelter.Domain.Auth;

namespace Shelter.Api.Features.Shelters.Update;

public static class UpdateShelterEndpoint
{
    public static RouteGroupBuilder MapUpdateShelter(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdateShelter")
            .WithSummary("Update a shelter")
            .Accepts<UpdateShelterRequest>("multipart/form-data")
            .Produces<ShelterDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AppPolicies.CanManageShelters)
            .DisableAntiforgery();

        return group;
    }

    private static async Task<Ok<ShelterDetailResponse>> HandleAsync(
        Guid id,
        [FromForm] UpdateShelterRequest request,
        IFormFileCollection newPictures,
        UpdateShelterHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var uploads = newPictures
            .Select(p => new FileUpload(p.OpenReadStream(), p.ContentType, p.FileName))
            .ToList();

        var response = await handler.HandleAsync(id, request, user.GetUserId(), uploads, cancellationToken);

        return TypedResults.Ok(response);
    }
}
