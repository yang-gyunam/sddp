namespace Sddp.Abstractions.DTOs;

/// <summary>
/// DTO
/// </summary>
public class QueryParameters
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    /// <summary>
    /// (1, default: 1)
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// (default: 10,: 100)
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => value
        };
    }

    /// <summary>
    ///
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// (asc/desc)
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// (→)
    /// </summary>
    public Dictionary<string, string> Filters { get; set; } = [];

    /// <summary>
    /// search
    /// </summary>
    public string? SearchKeyword { get; set; }

    /// <summary>
    /// Skip ()
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;
}

/// <summary>
///
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}
