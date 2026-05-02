using App.Common;
using App.Features.Shelters.Shared;
using App.Persistence;
using Shelter.Domain.Assets;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace App.Features.Shelters.Create;

public sealed class CreateShelterHandler(
    IShelterDbContext db,
    IClock clock,
    IFileStorage storage,
    ILogger<CreateShelterHandler> logger)
{
    public async Task<ShelterDetailResponse> HandleAsync(
        CreateShelterRequest request,
        Guid ownerId,
        IReadOnlyList<FileUpload> pictures,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        var shelter = ShelterEntity.Create(
            ownerId,
            request.Name,
            request.Description,
            request.Capacity,
            request.Latitude,
            request.Longitude,
            request.BookingPolicy,
            now);

        foreach (var picture in pictures)
        {
            var asset = await UploadAsync(ownerId, shelter.Id, picture, now, cancellationToken);
            db.Assets.Add(asset);
            shelter.AddPicture(asset.Id, caption: null, now);
        }

        db.Shelters.Add(shelter);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created shelter {ShelterId} for owner {OwnerId} with {PictureCount} pictures",
            shelter.Id, shelter.OwnerId, shelter.Pictures.Count);

        return ShelterDetailResponse.FromDomain(shelter, storage);
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
}
