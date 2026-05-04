using System.Security.Claims;
using App.Common;
using App.Features.Reviews.Create;
using App.Features.Reviews.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shelter.Api.Extensions;

namespace Shelter.Api.Features.Reviews.Create;

public static class CreateReviewEndpoint
{
    public static RouteGroupBuilder MapCreateReview(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("CreateReview")
            .WithSummary("Create a review for the shelter")
            .Accepts<CreateReviewRequest>("multipart/form-data")
            .Produces<ReviewDetailResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .DisableAntiforgery();

        return group;
    }

    private static async Task<Created<ReviewDetailResponse>> HandleAsync(
        Guid id,
        [FromForm] CreateReviewRequest request,
        IFormFileCollection pictures,
        CreateReviewHandler handler,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var uploads = pictures
            .Select(p => new FileUpload(p.OpenReadStream(), p.ContentType, p.FileName))
            .ToList();

        var response = await handler.HandleAsync(id, request, user.GetUserId(), uploads, cancellationToken);
        return TypedResults.Created($"/api/reviews/{response.Id}", response);
    }
}
