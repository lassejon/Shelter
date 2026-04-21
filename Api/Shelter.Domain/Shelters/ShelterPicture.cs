namespace Shelter.Domain.Shelters;

public class ShelterPicture
{
    public Guid Id { get; set; }
    public Guid ShelterId { get; set; }
    public string Url { get; set; } = null!;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
}
