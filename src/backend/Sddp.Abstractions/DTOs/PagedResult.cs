namespace Sddp.Abstractions.DTOs;

/// <summary>
/// Generic paginated result DTO.
/// </summary>
public class PagedResult<T>
{
    /// <summary>
    /// Items returned for the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number, starting from 1.
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Number of items requested per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of available pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Indicates whether a next page exists.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Indicates whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Creates an empty paginated result.
    /// </summary>
    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10) => new()
    {
        Items = [],
        TotalCount = 0,
        PageNumber = pageNumber,
        PageSize = pageSize
    };

    /// <summary>
    /// Creates a paginated result from the supplied values.
    /// </summary>
    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize) => new()
    {
        Items = items,
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
