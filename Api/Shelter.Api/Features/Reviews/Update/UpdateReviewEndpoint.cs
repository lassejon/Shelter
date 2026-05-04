using System.Security.Claims;
using App.Common;
using App.Features.Reviews.Shared;
using App.Features.Reviews.Update;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Reviews.Update;

public static class UpdateReviewEndpoint
{
    public static RouteGroupBuilder MapUpdateReview(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", HandleAsync)
            .WithName("UpdateReview")
            .WithSummary("Update one of your own reviews")
            .Accepts<UpdateReviewRequest>("multipart/form-data")
            .Produces<ReviewDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .DisableAntiforgery();

        return group;
    }

    private static async Task<Ok<ReviewDetailResponse>> HandleAsync(
        Guid id,
        [FromForm] UpdateReviewRequest request,
        IFormFileCollection newPictures,
        UpdateReviewHandler handler,
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
