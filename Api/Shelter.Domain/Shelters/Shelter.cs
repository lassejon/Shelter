namespace Shelter.Domain.Shelters;

public class Shelter
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public int Capacity { get; set; }
    public ShelterBookingPolicy BookingPolicy { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<ShelterPicture> Pictures { get; set; } = [];
}
