namespace SharpQuill.Application.Common.DTOs;

public record PagedResultDto<T>(
    IReadOnlyList<T> Data,
    PaginationMeta Meta
);

public record PaginationMeta(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
