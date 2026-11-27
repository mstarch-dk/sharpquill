namespace SharpQuill.Core.Common;

public class PaginationParams
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;

    public int GetValidatedPage() => Page < 1 ? 1 : Page;

    public int GetValidatedPageSize() => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize
    };

    public int GetOffset() => (GetValidatedPage() - 1) * GetValidatedPageSize();
}
