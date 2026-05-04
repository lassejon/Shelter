using App.Common;
using Shelter.Domain.Shelters;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace App.Features.Shelters.Shared;

public record ShelterDetailResponse(
    Guid Id,
    Guid OwnerId,
    string Name,
    string? Description,
    int Capacity,
    double Latitude,
    double Longitude,
    ShelterBookingPolicy BookingPolicy,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<PictureResponse> Pictures)
{
    public static ShelterDetailResponse FromDomain(ShelterEntity shelter, IFileStorage storage) => new(
        shelter.Id,
        shelter.OwnerId,
        shelter.Name,
        shelter.Description,
        shelter.Capacity,
        shelter.Latitude,
        shelter.Longitude,
        shelter.BookingPolicy,
        shelter.IsActive,
        shelter.CreatedAt,
        shelter.UpdatedAt,
        shelter.Pictures
            .OrderBy(p => p.SortOrder)
            .Select(p => new PictureResponse(p.Id, storage.GetPublicUrl(p.Asset.BlobKey), p.Caption, p.SortOrder))
            .ToList());
}
