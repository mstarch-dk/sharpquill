using System.Text.Json.Serialization;

namespace SharpQuill.Api.Infrastructure.Models;

public class ApiResponse<T>
{
    public T Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Meta { get; set; }

    public ApiResponse(T data, object? meta = null)
    {
        Data = data;
        Meta = meta;
    }
}

public class ApiErrorResponse
{
    public ApiError Error { get; set; }

    public ApiErrorResponse(string code, string message, IEnumerable<ApiErrorDetail>? details = null)
    {
        Error = new ApiError(code, message, details);
    }
}

public class ApiError
{
    public string Code { get; set; }
    public string Message { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ApiErrorDetail>? Details { get; set; }

    public ApiError(string code, string message, IEnumerable<ApiErrorDetail>? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }
}

public class ApiErrorDetail
{
    public string Field { get; set; }
    public string Message { get; set; }

    public ApiErrorDetail(string field, string message)
    {
        Field = field;
        Message = message;
    }
}
