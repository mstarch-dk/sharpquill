using SharpQuill.Api.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;

namespace SharpQuill.Api.Infrastructure.Auth;

/// <summary>
/// Answers failed authorization with the API's error envelope (<c>{ "error": { "code", "message" } }</c>)
/// instead of the framework's empty 401/403 responses.
/// </summary>
public sealed class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse("UNAUTHORIZED", "Authentication required"));
            return;
        }

        if (authorizeResult.Forbidden)
        {
            var roleMissing = authorizeResult.AuthorizationFailure?.FailedRequirements
                .Any(r => r is RolesAuthorizationRequirement) == true;
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse("FORBIDDEN", roleMissing ? "Admin role required" : "Access denied"));
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
