using Shelter.Domain.Shelters;
using ShelterModel = Shelter.Domain.Shelters.Shelter;
using Shelter.Domain.Users;

namespace Shelter.Domain.Pictures;

public enum PictureScope
{
    Shelter = 0,
    Review = 1
}

public class Picture
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public PictureScope Scope { get; set; }

    public Guid? ShelterId { get; set; }
    public ShelterModel? Shelter { get; set; }

    public Guid? ReviewId { get; set; }
    public Review? Review { get; set; }

    public string Url { get; set; } = null!;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
