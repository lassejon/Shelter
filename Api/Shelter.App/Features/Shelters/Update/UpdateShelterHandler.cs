using App.Common;
using App.Features.Reviews.Shared;
using App.Features.Shelters.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Assets;
using Shelter.Domain.Common;

namespace App.Features.Shelters.Update;

public sealed class UpdateShelterHandler(
    IShelterDbContext db,
    IClock clock,
    IFileStorage storage,
    AssetOrphanRecovery orphanRecovery,
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
                .ThenInclude(p => p.Asset)
            .FirstOrDefaultAsync(s => s.Id == shelterId, cancellationToken)
            ?? throw new DomainNotFoundException($"Shelter {shelterId} was not found.");

        if (!shelter.OwnedBy(userId))
            throw new DomainAuthorizationException("You can only update your own shelters.");

        var now = clock.UtcNow;

        var hasScalarChange = request.Name is not null
            || request.Description is not null
            || request.Capacity.HasValue
            || request.BookingPolicy.HasValue
            || request.BookingApprovalMode.HasValue;

        if (hasScalarChange)
        {
            shelter.UpdateDetails(
                request.Name                ?? shelter.Name,
                request.Description         ?? shelter.Description,
                request.Capacity            ?? shelter.Capacity,
                request.BookingPolicy       ?? shelter.BookingPolicy,
                request.BookingApprovalMode ?? shelter.BookingApprovalMode,
                now);
        }

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) shelter.Reactivate(now);
            else                        shelter.Deactivate(now);
        }

        var freedAssetIds = new List<Guid>();
        var deletedPictureIds = new List<Guid>();

        if (request.PictureIdsToDelete is { Count: > 0 })
        {
            foreach (var pictureId in request.PictureIdsToDelete)
            {
                var picture = shelter.Pictures.FirstOrDefault(p => p.Id == pictureId);
                if (picture is null) continue;
                freedAssetIds.Add(picture.AssetId);
                deletedPictureIds.Add(picture.Id);
                shelter.RemovePicture(pictureId, now);
            }
        }

        foreach (var picture in newPictures)
        {
            var asset = await UploadAsync(userId, shelter.Id, picture, now, cancellationToken);
            db.Assets.Add(asset);
            shelter.AddPicture(asset.Id, caption: null, now);
        }

        var blobsToDelete = await orphanRecovery.QueueOrphansFromDeletedPicturesAsync(
            freedAssetIds,
            deletedShelterPictureIds: deletedPictureIds,
            cancellationToken: cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await DeleteBlobsBestEffortAsync(blobsToDelete, cancellationToken);

        logger.LogInformation("Updated shelter {ShelterId} for owner {OwnerId}", shelter.Id, shelter.OwnerId);

        var summary = await LoadSummaryAsync(shelter.Id, cancellationToken);

        return ShelterDetailResponse.FromDomain(shelter, storage, summary);
    }

    private async Task<ReviewSummary> LoadSummaryAsync(Guid shelterId, CancellationToken cancellationToken)
    {
        var row = await db.Reviews
            .AsNoTracking()
            .Where(r => r.ShelterId == shelterId)
            .GroupBy(r => r.ShelterId)
            .Select(g => new { Average = g.Average(r => (double)(int)r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? ReviewSummary.Empty
            : new ReviewSummary(Math.Round(row.Average, 2), row.Count);
    }

    private async Task<Asset> UploadAsync(
        Guid ownerId,
        Guid shelterId,
        FileUpload picture,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var blobKey = $"shelters/{shelterId}/{Guid.NewGuid():N}";
        await storage.UploadAsync(blobKey, picture.Content, picture.ContentType, cancellationToken);
        return Asset.Create(ownerId, blobKey, picture.ContentType, now);
    }

    private async Task DeleteBlobsBestEffortAsync(
        IReadOnlyList<string> blobKeys,
        CancellationToken cancellationToken)
    {
        foreach (var blobKey in blobKeys)
        {
            try
            {
                await storage.DeleteAsync(blobKey, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to delete orphaned blob {BlobKey}; sweeper will retry",
                    blobKey);
            }
        }
    }
}
