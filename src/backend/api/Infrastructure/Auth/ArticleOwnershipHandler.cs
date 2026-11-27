using SharpQuill.Application.Articles.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace SharpQuill.Api.Infrastructure.Auth;

/// <summary>Resource requirement: the caller may modify (update, delete, publish, ...) this article.</summary>
public sealed class ModifyArticleRequirement : IAuthorizationRequirement
{
    public static readonly ModifyArticleRequirement Instance = new();
}

/// <summary>Admins may modify any article; other authors only the articles they wrote.</summary>
public sealed class ArticleOwnershipHandler : AuthorizationHandler<ModifyArticleRequirement, ArticleAdminDto>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ModifyArticleRequirement requirement, ArticleAdminDto article)
    {
        var user = context.User;
        if (user.IsAdminRole() || (user.IsAuthor() && article.Author?.Id == user.GetAuthorId()))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
