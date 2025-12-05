using Microsoft.AspNetCore.Http;
using NetTopologySuite.Geometries;
using Shelter.Domain.Shelters;
using ShelterModel = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Application.Requests;

public class CreateShelterRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public ShelterBookingPolicy BookingPolicy { get; set; }
    public List<IFormFile>? Pictures { get; set; }

    public ShelterModel ToDomain(Guid ownerId)
    {
        var shelter = new ShelterModel
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = Name,
            Description = Description,
            Capacity = Capacity,
            Location = new Point(Longitude, Latitude) { SRID = 4326 },
            BookingPolicy = BookingPolicy,
            IsActive = true,
            Pictures = []
        };

        return shelter;
    }
}

