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
    List<ShelterPictureResponse> Pictures)
{
    public static ShelterDetailResponse FromDomain(ShelterEntity shelter) => new(
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
            .Select(p => new ShelterPictureResponse(p.Id, p.Url, p.Caption, p.SortOrder))
            .ToList());
}
