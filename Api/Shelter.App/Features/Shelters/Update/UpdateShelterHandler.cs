using App.Common;
using App.Features.Shelters.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Shelters.Update;

public sealed class UpdateShelterHandler(
    IShelterDbContext db,
    IClock clock,
    ILogger<UpdateShelterHandler> logger)
{
    public async Task<ShelterDetailResponse> HandleAsync(
        Guid shelterId,
        UpdateShelterRequest request,
        Guid userId,
        IReadOnlyList<FileUpload> newPictures,
        CancellationToken cancellationToken)
    {
        var shelter = await db.Shelters
            .Include(s => s.Pictures)
            .FirstOrDefaultAsync(s => s.Id == shelterId, cancellationToken)
            ?? throw new DomainNotFoundException($"Shelter {shelterId} was not found.");

        if (!shelter.OwnedBy(userId))
            throw new DomainAuthorizationException("You can only update your own shelters.");

        var now = clock.UtcNow;

        var hasScalarChange = request.Name is not null
            || request.Description is not null
            || request.Capacity.HasValue;

        if (hasScalarChange)
        {
            shelter.UpdateDetails(
                request.Name        ?? shelter.Name,
                request.Description ?? shelter.Description,
                request.Capacity    ?? shelter.Capacity,
                shelter.BookingPolicy,
                now);
        }

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) shelter.Reactivate(now);
            else                        shelter.Deactivate(now);
        }

        if (request.PictureIdsToDelete is { Count: > 0 })
        {
            foreach (var pictureId in request.PictureIdsToDelete)
                shelter.RemovePicture(pictureId, now);
        }

        foreach (var picture in newPictures)
        {
            var url = $"https://mock.storage/shelters/{shelter.Id}/{Guid.NewGuid():N}-{picture.FileName}";
            shelter.AddPicture(url, caption: null, now);
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated shelter {ShelterId} for owner {OwnerId}", shelter.Id, shelter.OwnerId);

        return ShelterDetailResponse.FromDomain(shelter);
    }
}
