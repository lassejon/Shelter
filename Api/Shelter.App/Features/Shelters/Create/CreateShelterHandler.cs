using App.Common;
using App.Features.Shelters.Shared;
using App.Persistence;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace App.Features.Shelters.Create;

public sealed class CreateShelterHandler(
    IShelterDbContext db,
    IClock clock,
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
            var url = $"https://mock.storage/shelters/{shelter.Id}/{Guid.NewGuid():N}-{picture.FileName}";
            shelter.AddPicture(url, caption: null, now);
        }

        db.Shelters.Add(shelter);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created shelter {ShelterId} for owner {OwnerId} with {PictureCount} pictures",
            shelter.Id, shelter.OwnerId, shelter.Pictures.Count);

        return ShelterDetailResponse.FromDomain(shelter);
    }
}
