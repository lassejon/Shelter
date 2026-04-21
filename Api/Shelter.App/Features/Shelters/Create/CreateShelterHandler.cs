using App.Common;
using App.Features.Shelters.Shared;
using Shelter.Domain.Shelters;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace App.Features.Shelters.Create;

public sealed class CreateShelterHandler(ILogger<CreateShelterHandler> logger)
{
    public Task<ShelterDetailResponse> HandleAsync(
        CreateShelterRequest request,
        Guid ownerId,
        IReadOnlyList<FileUpload> pictures,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var shelterId = Guid.NewGuid();

        var shelter = new ShelterEntity
        {
            Id = shelterId,
            OwnerId = ownerId,
            Name = request.Name,
            Description = request.Description,
            Capacity = request.Capacity,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            BookingPolicy = request.BookingPolicy,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Pictures = pictures
                .Select((p, index) => new ShelterPicture
                {
                    Id = Guid.NewGuid(),
                    ShelterId = shelterId,
                    Url = $"https://mock.storage/shelters/{shelterId}/{Guid.NewGuid():N}-{p.FileName}",
                    Caption = null,
                    SortOrder = index,
                })
                .ToList(),
        };

        logger.LogInformation(
            "Mock-created shelter {ShelterId} for owner {OwnerId} with {PictureCount} pictures",
            shelter.Id, shelter.OwnerId, shelter.Pictures.Count);

        return Task.FromResult(ShelterDetailResponse.FromDomain(shelter));
    }
}
