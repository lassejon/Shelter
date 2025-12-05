using ShelterModel = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Application.Requests;

public record UpdateShelterRequest(
    string Name,
    string? Description,
    int Capacity,
    bool IsActive)
{
    public void ApplyTo(ShelterModel shelter)
    {
        shelter.Name = Name;
        shelter.Description = Description;
        shelter.Capacity = Capacity;
        shelter.IsActive = IsActive;
    }
}

