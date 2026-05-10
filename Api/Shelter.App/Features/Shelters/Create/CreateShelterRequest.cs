using Shelter.Domain.Shelters;

namespace App.Features.Shelters.Create;

public class CreateShelterRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public ShelterBookingPolicy BookingPolicy { get; set; }
    public BookingApprovalMode BookingApprovalMode { get; set; }
}
