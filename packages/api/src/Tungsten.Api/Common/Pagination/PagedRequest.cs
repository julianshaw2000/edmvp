namespace Tungsten.Api.Common.Pagination;

public record PagedRequest(int Page = 1, int PageSize = 20)
{
    public int Skip => (Page - 1) * PageSize;
}
