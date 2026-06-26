namespace QuickCart.Contracts.Common;

/// <summary>
/// Generic paginated API response envelope.
/// Returned by any list endpoint that supports page/pageSize query parameters.
/// </summary>
/// <typeparam name="T">The item type for this page.</typeparam>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasPrevious,
    bool HasNext)
{
    /// <summary>
    /// Builds a <see cref="PagedResponse{T}"/> and computes derived metadata.
    /// </summary>
    /// <param name="items">The items on the current page.</param>
    /// <param name="totalItems">Total items across all pages.</param>
    /// <param name="page">1-based current page number.</param>
    /// <param name="pageSize">Maximum items per page.</param>
    public static PagedResponse<T> From(
        IReadOnlyList<T> items,
        int totalItems,
        int page,
        int pageSize)
    {
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResponse<T>(
            Items:       items,
            Page:        page,
            PageSize:    pageSize,
            TotalItems:  totalItems,
            TotalPages:  totalPages,
            HasPrevious: page > 1,
            HasNext:     page < totalPages);
    }
}
