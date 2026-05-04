using Shelter.Domain.Reviews;

namespace App.Features.Reviews.Update;

public class UpdateReviewRequest
{
    public Rating? Rating { get; set; }
    public string? Comment { get; set; }
    public List<Guid>? PictureIdsToDelete { get; set; }
}
