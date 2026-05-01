namespace Shelter.Domain.Shelters;

public class ShelterPicture
{
    private ShelterPicture() { }

    internal ShelterPicture(Guid id, Guid shelterId, string url, string? caption, int sortOrder)
    {
        Id = id;
        ShelterId = shelterId;
        Url = url;
        Caption = caption;
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }
    public Guid ShelterId { get; private set; }
    public string Url { get; private set; } = null!;
    public string? Caption { get; private set; }
    public int SortOrder { get; private set; }
}
