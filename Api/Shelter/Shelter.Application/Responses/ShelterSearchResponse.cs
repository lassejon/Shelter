using Shelter.Domain.Shelters;
using ShelterModel = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Application.Responses;

public record ShelterSearchResponse(
    Guid Id,
    string Name,
    string? Description,
    int Capacity,
    double Latitude,
    double Longitude,
    ShelterBookingPolicy BookingPolicy,
    bool IsActive,
    List<PictureResponse> Pictures,
    ReviewSummary ReviewSummary)
{
    public static ShelterSearchResponse FromDomain(ShelterModel shelter) => new(
        shelter.Id,
        shelter.Name,
        shelter.Description,
        shelter.Capacity,
        shelter.Location.Y, // Latitude
        shelter.Location.X, // Longitude
        shelter.BookingPolicy,
        shelter.IsActive,
        shelter.Pictures
            .OrderBy(p => p.SortOrder)
            .Take(2)  // Only first 2 pictures
            .Select(PictureResponse.FromDomain)
            .ToList(),
        ReviewSummary.FromReviews(shelter.Reviews));
}
