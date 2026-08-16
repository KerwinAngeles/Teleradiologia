namespace Teleradiologia.Application.Common;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;

    public static PagedResult<T> Empty(int pageSize) => new([], 1, pageSize, 0);
}

public abstract record PageParams
{
    private const int MaxPageSize = 100;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    // Los valores llegan del cliente: se acotan acá para que una página de 100000 no
    // pueda tumbar la base.
    public int SafePageNumber => PageNumber < 1 ? 1 : PageNumber;

    public int SafePageSize => PageSize switch
    {
        < 1 => 20,
        > MaxPageSize => MaxPageSize,
        _ => PageSize,
    };

    public int Skip => (SafePageNumber - 1) * SafePageSize;
}
