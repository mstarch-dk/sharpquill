using SharpQuill.Api.Infrastructure.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace SharpQuill.Api.Infrastructure;

/// <summary>
/// Turns unhandled exceptions into the documented <c>{ "error": { code, message } }</c> format.
/// The exception itself is logged; its message and stack trace are never sent to the client.
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is BadHttpRequestException badRequest)
        {
            httpContext.Response.StatusCode = badRequest.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(
                new ApiErrorResponse("BAD_REQUEST", "The request could not be processed"), cancellationToken);
            return true;
        }

        _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new ApiErrorResponse("INTERNAL_ERROR", "An unexpected error occurred"), cancellationToken);
        return true;
    }
}
