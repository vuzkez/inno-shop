namespace Inno_Shop.Products.Application.Common
{
    /// <summary>One page of results plus the metadata a client needs to build pagination.</summary>
    public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
    {
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
