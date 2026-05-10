using App.Common;
using App.Features.Reviews.Shared;
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
    List<PictureResponse> Pictures,
    ReviewSummary ReviewSummary)
{
    public static ShelterDetailResponse FromDomain(
        ShelterEntity shelter,
        IFileStorage storage,
        ReviewSummary reviewSummary) => new(
        shelter.Id,
        shelter.OwnerId,
        shelter.Name,
        shelter.Description,
        shelter.Capacity,
        shelter.Location.Y,
        shelter.Location.X,
        shelter.BookingPolicy,
        shelter.IsActive,
        shelter.CreatedAt,
        shelter.UpdatedAt,
        shelter.Pictures
            .OrderBy(p => p.SortOrder)
            .Select(p => new PictureResponse(p.Id, storage.GetPublicUrl(p.Asset.BlobKey), p.Caption, p.SortOrder))
            .ToList(),
        reviewSummary);
}
