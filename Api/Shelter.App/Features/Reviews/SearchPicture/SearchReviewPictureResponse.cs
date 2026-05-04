using App.Common;
using App.Features.Reviews.Shared;

namespace App.Features.Reviews.SearchPicture;

public record SearchReviewPictureResponse(
    List<PictureResponse> Pictures,
    Pagination Pagination);
