using SharpQuill.Api.Infrastructure.Filters;
using SharpQuill.Api.Infrastructure.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace SharpQuill.Api.Tests.Services;

/// <summary>
/// ValidationFilter turns invalid model state into the API's VALIDATION_ERROR response with camelCase field names.
/// </summary>
public class ValidationFilterTests
{
    private static ActionExecutingContext Context(Action<Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary>? addErrors = null)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        addErrors?.Invoke(actionContext.ModelState);
        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), controller: new object());
    }

    [Fact]
    public void ValidModelState_LeavesTheActionToRun()
    {
        var context = Context(state =>
        {
            state.SetModelValue("Title", "ok", "ok");
            state.MarkFieldValid("Title");
        });

        new ValidationFilter().OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void InvalidModelState_ReturnsBadRequestWithCamelCaseFields()
    {
        var context = Context(state =>
        {
            state.AddModelError("Title", "Title is required");
            state.AddModelError("Title", "Title is too long");
            state.AddModelError("Layout.Sections", "Invalid layout");
            state.AddModelError("", "Body is not valid JSON");
            state.AddModelError("Items..Name", "Odd key");
            state.SetModelValue("Slug", "fine", "fine");
        });

        new ValidationFilter().OnActionExecuting(context);

        var result = context.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = result.Value.Should().BeOfType<ApiErrorResponse>().Subject.Error;
        error.Code.Should().Be("VALIDATION_ERROR");
        error.Details!.Select(d => (d.Field, d.Message)).Should().BeEquivalentTo(new[]
        {
            ("title", "Title is required"),
            ("title", "Title is too long"),
            ("layout.sections", "Invalid layout"),
            ("", "Body is not valid JSON"),
            ("items..name", "Odd key")
        });
    }

    [Fact]
    public void OnActionExecuted_DoesNothing()
    {
        var context = Context();
        var executed = new ActionExecutedContext(context, [], controller: new object());

        new ValidationFilter().OnActionExecuted(executed);

        executed.Result.Should().BeNull();
    }
}
