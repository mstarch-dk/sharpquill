using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Core.Articles;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

/// <summary>
/// A "user" role author must not reach admin-only areas or modify articles owned by others.
/// </summary>
[Collection("Database")]
public class AdminRoleEnforcementTests
{
    private const int AdminAuthorId = 1;
    private const int OtherAuthorId = 2;

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _admin;
    private readonly HttpClient _user;

    public AdminRoleEnforcementTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _admin = AuthHelper.CreateAuthenticatedClient(fixture.Factory.CreateClient(), AdminAuthorId, "admin");
        _user = AuthHelper.CreateAuthenticatedClient(fixture.Factory.CreateClient(), OtherAuthorId, "user");
    }

    [Fact]
    public async Task WebsiteUsers_AsUserRole_ReturnsForbidden()
    {
        var response = await _user.GetAsync("/api/admin/website-users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Export_AsUserRole_ReturnsForbidden()
    {
        var response = await _user.PostAsJsonAsync("/api/admin/export", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSettings_AsUserRole_ReturnsForbidden()
    {
        var response = await _user.PutAsJsonAsync("/api/admin/settings", new { SiteName = "Hijacked" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSettings_AsUserRole_StillAllowed()
    {
        var response = await _user.GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Import_AsUserRole_ReturnsForbidden()
    {
        var response = await _user.PostAsync("/api/admin/import", new MultipartFormDataContent());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateAndDeleteArticle_ByNonOwnerUserRole_ReturnsForbidden()
    {
        var article = await CreateArticleAsAdminAsync();

        var update = await _user.PutAsJsonAsync($"/api/admin/articles/{article.Id}", new { Title = "Hijacked", Slug = $"hijack-{Guid.NewGuid():N}", Content = "x", Status = ArticleStatus.Draft });
        var delete = await _user.DeleteAsync($"/api/admin/articles/{article.Id}");

        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var stillThere = await _admin.GetAsync($"/api/admin/articles/{article.Id}");
        stillThere.StatusCode.Should().Be(HttpStatusCode.OK);

        await _admin.DeleteAsync($"/api/admin/articles/{article.Id}");
    }

    [Fact]
    public async Task DeleteArticle_ByOwnerUserRole_Succeeds()
    {
        var slug = $"owned-{Guid.NewGuid():N}";
        var create = await _user.PostAsJsonAsync("/api/admin/articles",
            new { Title = "Mine", Slug = slug, Content = "x" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>())!.Data;

        var delete = await _user.DeleteAsync($"/api/admin/articles/{created.Id}");

        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteArticle_ByAdmin_CanDeleteOthersArticles()
    {
        var create = await _user.PostAsJsonAsync("/api/admin/articles",
            new { Title = "Theirs", Slug = $"theirs-{Guid.NewGuid():N}", Content = "x" });
        var created = (await create.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>())!.Data;

        var delete = await _admin.DeleteAsync($"/api/admin/articles/{created.Id}");

        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<ArticleAdminDto> CreateArticleAsAdminAsync()
    {
        var response = await _admin.PostAsJsonAsync("/api/admin/articles",
            new { Title = "Admin article", Slug = $"admin-{Guid.NewGuid():N}", Content = "x" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>())!.Data;
    }
}
