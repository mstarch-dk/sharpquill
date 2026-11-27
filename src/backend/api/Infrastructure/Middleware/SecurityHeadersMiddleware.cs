namespace SharpQuill.Api.Infrastructure.Middleware;

/// <summary>
/// Adds baseline security headers to API responses unless a more specific handler already set them
/// (e.g. the uploads handler sets its own Content-Security-Policy). Transport-level headers such as HSTS
/// belong on the TLS-terminating reverse proxy; see docs/security-headers.md.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
            return Task.CompletedTask;
        });

        return _next(context);
    }
}
