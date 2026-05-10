using Shelter.Domain.Shelters;

namespace App.Features.Shelters.Update;

public class UpdateShelterRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public ShelterBookingPolicy? BookingPolicy { get; set; }
    public BookingApprovalMode? BookingApprovalMode { get; set; }
    public bool? IsActive { get; set; }
    public List<Guid>? PictureIdsToDelete { get; set; }
}
