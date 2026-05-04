using Shelter.Domain.Reviews;

namespace App.Features.Reviews.Create;

public class CreateReviewRequest
{
    public Rating Rating { get; set; }
    public string? Comment { get; set; }
}
