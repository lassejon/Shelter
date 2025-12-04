using Shelter.Domain.Pictures;
using Shelter.Domain.Users;

namespace Shelter.Domain.Shelters;

public enum Rating
{
    Poor = 1,
    Lacking = 2,
    Fair = 3,
    Good = 4,
    Excellent = 5
}

public class Review
{
    public Guid Id { get; set; }

    public Guid ShelterId { get; set; }
    public Shelter Shelter { get; set; } = null!;

    public Guid ReviewerId { get; set; }
    public User Reviewer { get; set; } = null!;

    public Rating Rating { get; set; }
    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<Picture> Pictures { get; set; } = new();
}
