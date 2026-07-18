namespace SIGO.Objects.Contracts;

/// <summary>
/// Represents one bounded page of a collection.
/// </summary>
/// <typeparam name="T">The resource type contained in the page.</typeparam>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    /// <summary>
    /// Creates a page from an already scoped collection.
    /// </summary>
    public static PagedResponse<T> Create(
        IEnumerable<T> source,
        PaginationRequest pagination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(pagination);

        var materialized = source as IReadOnlyCollection<T> ?? source.ToArray();
        var totalItems = materialized.Count;
        var skip = (long)(pagination.Page - 1) * pagination.PageSize;
        var items = skip >= totalItems
            ? Array.Empty<T>()
            : materialized
                .Skip((int)skip)
                .Take(pagination.PageSize)
                .ToArray();
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pagination.PageSize);

        return new PagedResponse<T>(
            items,
            pagination.Page,
            pagination.PageSize,
            totalItems,
            totalPages);
    }
}
