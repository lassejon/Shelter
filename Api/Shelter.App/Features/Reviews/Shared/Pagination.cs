namespace App.Features.Reviews.Shared;

public record Pagination(int Page, int PageSize, int TotalCount, int TotalPages)
{
    public static Pagination From(int page, int pageSize, int totalCount) => new(
        page,
        pageSize,
        totalCount,
        pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
}
