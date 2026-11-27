using SharpQuill.Api.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SharpQuill.Api.Infrastructure.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new ApiErrorDetail(
                    ToCamelCase(x.Key),
                    e.ErrorMessage
                )))
                .ToList();

            var response = new ApiErrorResponse("VALIDATION_ERROR", "Validation failed", errors);
            context.Result = new BadRequestObjectResult(response);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        // Handle nested properties like "Property.SubProperty"
        var parts = str.Split('.');
        return string.Join(".", parts.Select(part =>
            string.IsNullOrEmpty(part) ? part : char.ToLowerInvariant(part[0]) + part[1..]
        ));
    }
}
