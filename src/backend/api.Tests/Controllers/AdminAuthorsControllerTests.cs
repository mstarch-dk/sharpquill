using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Auth.DTOs;
using SharpQuill.Application.Authors.DTOs;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

/// <summary>
/// Author (admin account) management: CRUD for admins, rejection of everyone else, and the protection rules in
/// AdminAuthorService (unique email, valid role, no self role change, no self delete, sessions revoked when the
/// role or password changes). Each test creates its own authors so the seeded authors stay untouched.
/// </summary>
[Collection("Database")]
public class AdminAuthorsControllerTests
{
    private const string BaseUrl = "/api/admin/authors";
    private const string Password = "a-long-test-password";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _admin;

    public AdminAuthorsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _admin = AuthHelper.CreateAuthenticatedClient(fixture.Factory.CreateClient(), TestSeedData.AdminAuthorId, "admin");
    }

    // ---- List and get ----

    [Fact]
    public async Task GetAuthors_ReturnsSeededAndCreatedAuthorsOrderedByName()
    {
        var created = await CreateAuthorAsync(name: "Zz Last Author");

        var response = await _admin.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authors = (await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<AuthorAdminDto>>>())!.Data;
        authors.Should().Contain(a => a.Id == TestSeedData.AdminAuthorId && a.Email == TestSeedData.AdminEmail);
        authors.Should().Contain(a => a.Id == created.Id);
        authors.Select(a => a.Name).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task GetAuthors_DoesNotExposePasswordHashes()
    {
        var response = await _admin.GetAsync(BaseUrl);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainEquivalentOf("password");
        body.Should().NotContain("$2a$");
    }

    [Fact]
    public async Task GetAuthor_ReturnsAllAdminFields()
    {
        var created = await CreateAuthorAsync(role: "user", bio: "Writes things", avatarUrl: "/uploads/a.png",
            thumbprint: UniqueThumbprint());

        var response = await _admin.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var author = (await response.Content.ReadFromJsonAsync<ApiResponse<AuthorAdminDto>>())!.Data;
        author.Should().BeEquivalentTo(created, o => o
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(1)))
            .WhenTypeIs<DateTime>());
        author.Role.Should().Be("user");
        author.Bio.Should().Be("Writes things");
        author.AvatarUrl.Should().Be("/uploads/a.png");
        author.CertificateThumbprint.Should().Be(created.CertificateThumbprint);
        author.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetAuthor_Unknown_ReturnsNotFound()
    {
        var response = await _admin.GetAsync($"{BaseUrl}/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ErrorCodeAsync(response)).Should().Be("NOT_FOUND");
    }

    // ---- Create ----

    [Fact]
    public async Task CreateAuthor_ReturnsCreatedWithLocation_AndStoresBcryptHash()
    {
        var email = UniqueEmail();

        var response = await _admin.PostAsJsonAsync(BaseUrl, NewAuthor(email, role: "user"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await response.Content.ReadFromJsonAsync<ApiResponse<AuthorAdminDto>>())!.Data;
        created.Id.Should().BePositive();
        created.Email.Should().Be(email);
        created.Role.Should().Be("user");
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        response.Headers.Location!.ToString().Should().EndWith($"{BaseUrl}/{created.Id}");

        var hash = await PasswordHashAsync(created.Id);
        hash.Should().StartWith("$2").And.NotContain(Password);
        BCrypt.Net.BCrypt.Verify(Password, hash).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAuthor_NewAuthorCanLogIn()
    {
        var created = await CreateAuthorAsync(role: "user");

        var login = await _fixture.Factory.CreateClient().WithUniqueClientIp().PostAsJsonAsync("/api/admin/auth/login",
            new { created.Email, Password });

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await login.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>())!.Data;
        result.Author.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task CreateAuthor_DuplicateEmail_ReturnsBadRequest()
    {
        var response = await _admin.PostAsJsonAsync(BaseUrl, NewAuthor(TestSeedData.AdminEmail));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).Should().Be("CREATION_FAILED");
        (await ErrorMessageAsync(response)).Should().Contain("email already exists");
    }

    [Theory]
    [InlineData("superadmin")]
    [InlineData("Admin")]
    [InlineData("")]
    public async Task CreateAuthor_InvalidRole_ReturnsBadRequest(string role)
    {
        var email = UniqueEmail();

        var response = await _admin.PostAsJsonAsync(BaseUrl, NewAuthor(email, role: role));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await AuthorCountAsync(email)).Should().Be(0);
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("Password")]
    [InlineData("Name")]
    [InlineData("Role")]
    public async Task CreateAuthor_MissingRequiredField_ReturnsBadRequest(string missing)
    {
        var email = UniqueEmail();
        var body = new Dictionary<string, object?>
        {
            ["Email"] = email, ["Password"] = Password, ["Name"] = "Someone", ["Role"] = "user"
        };
        body.Remove(missing);

        var response = await _admin.PostAsJsonAsync(BaseUrl, body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await AuthorCountAsync(email)).Should().Be(0);
    }

    // ---- Update ----

    [Fact]
    public async Task UpdateAuthor_ChangesFieldsAndSetsUpdatedAt()
    {
        var created = await CreateAuthorAsync(role: "user");
        var newEmail = UniqueEmail();
        var thumbprint = UniqueThumbprint();

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}", new AuthorUpdateDto(
            newEmail, "Renamed", "user", null, "New bio", "/uploads/new.png", thumbprint));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<ApiResponse<AuthorAdminDto>>())!.Data;
        updated.Email.Should().Be(newEmail);
        updated.Name.Should().Be("Renamed");
        updated.Bio.Should().Be("New bio");
        updated.AvatarUrl.Should().Be("/uploads/new.png");
        updated.CertificateThumbprint.Should().Be(thumbprint);
        updated.UpdatedAt.Should().NotBeNull();

        var reloaded = await GetAuthorAsync(created.Id);
        reloaded.Should().BeEquivalentTo(updated, o => o.Excluding(a => a.UpdatedAt));
        reloaded.UpdatedAt.Should().BeCloseTo(updated.UpdatedAt!.Value, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task UpdateAuthor_ClearsOptionalFields()
    {
        var created = await CreateAuthorAsync(bio: "Bio", avatarUrl: "/uploads/a.png", thumbprint: UniqueThumbprint());

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email, created.Name, created.Role, null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reloaded = await GetAuthorAsync(created.Id);
        reloaded.Bio.Should().BeNull();
        reloaded.AvatarUrl.Should().BeNull();
        reloaded.CertificateThumbprint.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UpdateAuthor_WithoutPassword_KeepsPasswordHash(string? password)
    {
        var created = await CreateAuthorAsync();
        var hashBefore = await PasswordHashAsync(created.Id);

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email, "Renamed", created.Role, password, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await PasswordHashAsync(created.Id)).Should().Be(hashBefore);
    }

    [Fact]
    public async Task UpdateAuthor_WithPassword_RehashesPassword()
    {
        var created = await CreateAuthorAsync();
        const string newPassword = "another-long-password";

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email, created.Name, created.Role, newPassword, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var hash = await PasswordHashAsync(created.Id);
        BCrypt.Net.BCrypt.Verify(newPassword, hash).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify(Password, hash).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAuthor_EmailOfAnotherAuthor_ReturnsBadRequest()
    {
        var created = await CreateAuthorAsync();

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(TestSeedData.AdminEmail, created.Name, created.Role, null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).Should().Be("UPDATE_FAILED");
        (await ErrorMessageAsync(response)).Should().Contain("email already exists");
        (await GetAuthorAsync(created.Id)).Email.Should().Be(created.Email);
    }

    [Fact]
    public async Task UpdateAuthor_KeepingOwnEmail_IsNotADuplicate()
    {
        var created = await CreateAuthorAsync();

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email, "Same email", created.Role, null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateAuthor_InvalidRole_ReturnsBadRequest()
    {
        var created = await CreateAuthorAsync(role: "user");

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email, created.Name, "owner", null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Contain("Invalid role");
        (await GetAuthorAsync(created.Id)).Role.Should().Be("user");
    }

    [Fact]
    public async Task UpdateAuthor_Unknown_ReturnsNotFound()
    {
        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/999999",
            new AuthorUpdateDto(UniqueEmail(), "Nobody", "user", null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ErrorCodeAsync(response)).Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAuthor_AdminCanPromoteAndDemoteOthers()
    {
        var created = await CreateAuthorAsync(role: "user");

        var promote = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email, created.Name, "admin", null, null, null, null));
        promote.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetAuthorAsync(created.Id)).Role.Should().Be("admin");

        var demote = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email, created.Name, "user", null, null, null, null));
        demote.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetAuthorAsync(created.Id)).Role.Should().Be("user");
    }

    // ---- Protection rules ----

    [Fact]
    public async Task UpdateAuthor_OwnRole_ReturnsBadRequestAndKeepsRole()
    {
        // The only way to lose the last admin would be for that admin to demote or delete themselves;
        // both are blocked, so an admin always remains.
        var self = await CreateAuthorAsync(role: "admin");
        var selfClient = AuthHelper.CreateAuthenticatedClient(_fixture.Factory.CreateClient(), self.Id, "admin");

        var response = await selfClient.PutAsJsonAsync($"{BaseUrl}/{self.Id}",
            new AuthorUpdateDto(self.Email, self.Name, "user", null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).Should().Be("UPDATE_FAILED");
        (await ErrorMessageAsync(response)).Should().Be("You cannot change your own role");
        (await GetAuthorAsync(self.Id)).Role.Should().Be("admin");
    }

    [Fact]
    public async Task UpdateAuthor_OwnProfileWithSameRole_Succeeds()
    {
        var self = await CreateAuthorAsync(role: "admin");
        var selfClient = AuthHelper.CreateAuthenticatedClient(_fixture.Factory.CreateClient(), self.Id, "admin");

        var response = await selfClient.PutAsJsonAsync($"{BaseUrl}/{self.Id}",
            new AuthorUpdateDto(self.Email, "New own name", "admin", null, "Own bio", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetAuthorAsync(self.Id)).Name.Should().Be("New own name");
    }

    [Fact]
    public async Task DeleteAuthor_Self_ReturnsBadRequestAndKeepsAccount()
    {
        var self = await CreateAuthorAsync(role: "admin");
        var selfClient = AuthHelper.CreateAuthenticatedClient(_fixture.Factory.CreateClient(), self.Id, "admin");

        var response = await selfClient.DeleteAsync($"{BaseUrl}/{self.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).Should().Be("DELETE_FAILED");
        (await ErrorMessageAsync(response)).Should().Be("You cannot delete your own account");
        (await AuthorCountAsync(self.Email)).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAuthor_RoleChange_RevokesRefreshTokens()
    {
        var created = await CreateAuthorAsync(role: "admin");
        await InsertRefreshTokenAsync(created.Id);

        await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email, created.Name, "user", null, null, null, null));

        (await ActiveRefreshTokensAsync(created.Id)).Should().Be(0);
    }

    [Fact]
    public async Task UpdateAuthor_PasswordChange_RevokesRefreshTokens()
    {
        var created = await CreateAuthorAsync();
        await InsertRefreshTokenAsync(created.Id);

        await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email, created.Name, created.Role, "a-brand-new-password", null, null, null));

        (await ActiveRefreshTokensAsync(created.Id)).Should().Be(0);
    }

    [Fact]
    public async Task UpdateAuthor_ProfileOnlyChange_KeepsRefreshTokens()
    {
        var created = await CreateAuthorAsync();
        await InsertRefreshTokenAsync(created.Id);

        await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email, "Just a rename", created.Role, null, "bio", null, null));

        (await ActiveRefreshTokensAsync(created.Id)).Should().Be(1);
    }

    // ---- Input rules: password policy, email format and case, name ----

    [Theory]
    [InlineData("")]
    [InlineData("            ")]
    [InlineData("x")]
    [InlineData("elevenchars")]
    public async Task CreateAuthor_PasswordBelowPolicy_ReturnsBadRequest(string password)
    {
        var email = UniqueEmail();

        var response = await _admin.PostAsJsonAsync(BaseUrl,
            new AuthorCreateDto(email, password, "Weak", "admin", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).Should().Be("CREATION_FAILED");
        (await ErrorMessageAsync(response)).Should().Be("Password must be at least 12 characters and not only spaces");
        (await AuthorCountAsync(email)).Should().Be(0);
    }

    [Fact]
    public async Task CreateAuthor_PasswordOfExactlyTwelveCharacters_IsAccepted()
    {
        var response = await _admin.PostAsJsonAsync(BaseUrl,
            new AuthorCreateDto(UniqueEmail(), "twelve-chars", "Exact", "user", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("y")]
    [InlineData("            ")]
    [InlineData("elevenchars")]
    public async Task UpdateAuthor_NewPasswordBelowPolicy_ReturnsBadRequestAndKeepsPassword(string password)
    {
        var created = await CreateAuthorAsync();
        var hashBefore = await PasswordHashAsync(created.Id);

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email, created.Name, created.Role, password, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).Should().Be("UPDATE_FAILED");
        (await ErrorMessageAsync(response)).Should().Contain("at least 12 characters");
        (await PasswordHashAsync(created.Id)).Should().Be(hashBefore);
    }

    [Theory]
    [InlineData("ADMIN@example.com")]
    [InlineData("Admin@Example.COM")]
    [InlineData("  admin@example.com  ")]
    public async Task CreateAuthor_EmailDifferingOnlyByCaseOrSpaces_IsADuplicate(string email)
    {
        var response = await _admin.PostAsJsonAsync(BaseUrl, NewAuthor(email));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Contain("email already exists");
        (await TestSql.ScalarAsync<long>("SELECT COUNT(*) FROM authors WHERE lower(trim(email)) = 'admin@example.com'"))
            .Should().Be(1);
    }

    [Fact]
    public async Task UpdateAuthor_EmailOfAnotherAuthorInOtherCase_IsADuplicate()
    {
        var created = await CreateAuthorAsync();

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(TestSeedData.AdminEmail.ToUpperInvariant(), created.Name, created.Role, null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Contain("email already exists");
        (await GetAuthorAsync(created.Id)).Email.Should().Be(created.Email);
    }

    [Fact]
    public async Task UpdateAuthor_OwnEmailInOtherCase_IsNotADuplicateAndIsStoredLowerCase()
    {
        var created = await CreateAuthorAsync();

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(created.Email.ToUpperInvariant(), created.Name, created.Role, null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetAuthorAsync(created.Id)).Email.Should().Be(created.Email);
    }

    [Fact]
    public async Task CreateAuthor_StoresEmailTrimmedAndLowerCase_AndNameTrimmed()
    {
        var local = $"Mixed.Case-{Guid.NewGuid():N}";

        var response = await _admin.PostAsJsonAsync(BaseUrl,
            new AuthorCreateDto($"  {local}@Example.COM ", Password, "  Spaced Name  ", "user", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await response.Content.ReadFromJsonAsync<ApiResponse<AuthorAdminDto>>())!.Data;
        created.Email.Should().Be($"{local.ToLowerInvariant()}@example.com");
        created.Name.Should().Be("Spaced Name");
        (await GetAuthorAsync(created.Id)).Email.Should().Be(created.Email);
    }

    [Fact]
    public async Task Login_WithEmailInOtherCase_Succeeds()
    {
        var created = await CreateAuthorAsync();

        var login = await _fixture.Factory.CreateClient().WithUniqueClientIp().PostAsJsonAsync("/api/admin/auth/login",
            new { Email = $" {created.Email.ToUpperInvariant()} ", Password });

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        (await login.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>())!.Data.Author.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Database_RejectsEmailsDifferingOnlyByCase()
    {
        // The unique index on lower(email) backs up the service check, for example against concurrent creates.
        var act = () => TestSql.ExecuteAsync(@"
            INSERT INTO authors (email, password_hash, name, role, created_at)
            VALUES ('ADMIN@EXAMPLE.COM', 'x', 'Clash', 'user', NOW())");

        (await act.Should().ThrowAsync<Npgsql.PostgresException>()).Which.SqlState.Should().Be("23505");
    }

    [Theory]
    [InlineData("not-an-email", "Name")]
    [InlineData("a@b", "Name")]
    [InlineData("two words@example.com", "Name")]
    [InlineData("   ", "Name")]
    [InlineData(null, "   ")]
    [InlineData(null, "")]
    public async Task CreateAuthor_InvalidEmailOrBlankName_ReturnsBadRequest(string? email, string name)
    {
        var uniqueEmail = UniqueEmail();

        var response = await _admin.PostAsJsonAsync(BaseUrl,
            new AuthorCreateDto(email ?? uniqueEmail, Password, name, "user", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await AuthorCountAsync(uniqueEmail)).Should().Be(0);
    }

    [Theory]
    [InlineData("not-an-email", "Name", "Email must be a valid email address")]
    [InlineData(null, "   ", "Name is required")]
    public async Task UpdateAuthor_InvalidEmailOrBlankName_ReturnsBadRequestAndKeepsAuthor(string? email, string name, string message)
    {
        var created = await CreateAuthorAsync();

        var response = await _admin.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new AuthorUpdateDto(email ?? created.Email, name, created.Role, null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorMessageAsync(response)).Should().Be(message);
        (await GetAuthorAsync(created.Id)).Should().BeEquivalentTo(created, o => o
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(1)))
            .WhenTypeIs<DateTime>());
    }

    // ---- Delete ----

    [Fact]
    public async Task DeleteAuthor_RemovesAuthorAndTheirSessions()
    {
        var created = await CreateAuthorAsync();
        await InsertRefreshTokenAsync(created.Id);

        var response = await _admin.DeleteAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _admin.GetAsync($"{BaseUrl}/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await TestSql.ScalarAsync<long>("SELECT COUNT(*) FROM refresh_tokens WHERE author_id = @Id",
            ("Id", created.Id))).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAuthor_KeepsTheirArticlesWithoutAuthor()
    {
        var created = await CreateAuthorAsync(role: "user");
        var authorClient = AuthHelper.CreateAuthenticatedClient(_fixture.Factory.CreateClient(), created.Id, "user");
        var article = await authorClient.PostAsJsonAsync("/api/admin/articles",
            new { Title = "Orphan", Slug = $"orphan-{Guid.NewGuid():N}", Content = "x" });
        article.StatusCode.Should().Be(HttpStatusCode.Created);
        var articleId = (await article.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>())!.Data.GetProperty("id").GetInt32();

        var response = await _admin.DeleteAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var authorId = await TestSql.ScalarAsync<object>("SELECT author_id FROM articles WHERE id = @Id", ("Id", articleId));
        authorId.Should().Be(DBNull.Value);

        await _admin.DeleteAsync($"/api/admin/articles/{articleId}");
    }

    [Fact]
    public async Task DeleteAuthor_Unknown_ReturnsNotFound()
    {
        var response = await _admin.DeleteAsync($"{BaseUrl}/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ErrorCodeAsync(response)).Should().Be("NOT_FOUND");
    }

    // ---- Who may call ----

    public static TheoryData<string, string> Endpoints => new()
    {
        { "GET", BaseUrl },
        { "GET", $"{BaseUrl}/{TestSeedData.AdminAuthorId}" },
        { "POST", BaseUrl },
        { "PUT", $"{BaseUrl}/{TestSeedData.CertAuthorId}" },
        { "DELETE", $"{BaseUrl}/{TestSeedData.CertAuthorId}" },
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Endpoint_AsUserRole_ReturnsForbidden(string method, string url)
    {
        var user = AuthHelper.CreateAuthenticatedClient(_fixture.Factory.CreateClient(), TestSeedData.CertAuthorId, "user");

        var response = await user.SendAsync(BuildRequest(method, url));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertSeededAuthorsUnchangedAsync();
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Endpoint_Unauthenticated_ReturnsUnauthorized(string method, string url)
    {
        var anon = _fixture.Factory.CreateClient();

        var response = await anon.SendAsync(BuildRequest(method, url));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertSeededAuthorsUnchangedAsync();
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Endpoint_WithWebsiteUserToken_ReturnsUnauthorized(string method, string url)
    {
        // Website reader tokens carry a different audience and must never reach the admin API.
        var reader = AuthHelper.CreateWebsiteUserAuthenticatedClient(_fixture.Factory.CreateClient());

        var response = await reader.SendAsync(BuildRequest(method, url));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAuthor_AsUserRole_DoesNotCreate()
    {
        var user = AuthHelper.CreateAuthenticatedClient(_fixture.Factory.CreateClient(), TestSeedData.CertAuthorId, "user");
        var email = UniqueEmail();

        var response = await user.PostAsJsonAsync(BaseUrl, NewAuthor(email, role: "admin"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await AuthorCountAsync(email)).Should().Be(0);
    }

    // ---- Helpers ----

    private static HttpRequestMessage BuildRequest(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new AuthorUpdateDto(
                $"hijack-{Guid.NewGuid():N}@example.com", "Hijacked", "admin", "hijacked-password", null, null, null));
        }
        return request;
    }

    private async Task AssertSeededAuthorsUnchangedAsync()
    {
        var admin = await GetAuthorAsync(TestSeedData.AdminAuthorId);
        admin.Email.Should().Be(TestSeedData.AdminEmail);
        var cert = await GetAuthorAsync(TestSeedData.CertAuthorId);
        cert.Email.Should().Be(TestSeedData.CertAuthorEmail);
        cert.Name.Should().NotBe("Hijacked");
    }

    private static string UniqueEmail() => $"author-{Guid.NewGuid():N}@example.com";

    private static string UniqueThumbprint() => Guid.NewGuid().ToString("N").ToUpperInvariant();

    private static AuthorCreateDto NewAuthor(string email, string name = "Test Author", string role = "user",
        string? bio = null, string? avatarUrl = null, string? thumbprint = null) =>
        new(email, Password, name, role, bio, avatarUrl, thumbprint);

    private async Task<AuthorAdminDto> CreateAuthorAsync(string name = "Test Author", string role = "user",
        string? bio = null, string? avatarUrl = null, string? thumbprint = null)
    {
        var response = await _admin.PostAsJsonAsync(BaseUrl, NewAuthor(UniqueEmail(), name, role, bio, avatarUrl, thumbprint));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<AuthorAdminDto>>())!.Data;
    }

    private async Task<AuthorAdminDto> GetAuthorAsync(int id)
    {
        var response = await _admin.GetAsync($"{BaseUrl}/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<AuthorAdminDto>>())!.Data;
    }

    private static Task<string> PasswordHashAsync(int authorId) =>
        TestSql.ScalarAsync<string>("SELECT password_hash FROM authors WHERE id = @Id", ("Id", authorId));

    private static Task<long> AuthorCountAsync(string email) =>
        TestSql.ScalarAsync<long>("SELECT COUNT(*) FROM authors WHERE email = @Email", ("Email", email));

    private static Task InsertRefreshTokenAsync(int authorId) =>
        TestSql.ExecuteAsync(@"
            INSERT INTO refresh_tokens (token, author_id, expires_at, created_at)
            VALUES (@Token, @AuthorId, NOW() + INTERVAL '1 day', NOW())",
            ("Token", $"authors-test-{Guid.NewGuid():N}"), ("AuthorId", authorId));

    private static Task<long> ActiveRefreshTokensAsync(int authorId) =>
        TestSql.ScalarAsync<long>(
            "SELECT COUNT(*) FROM refresh_tokens WHERE author_id = @Id AND revoked_at IS NULL", ("Id", authorId));

    private static Task<string> ErrorCodeAsync(HttpResponseMessage response) => ErrorFieldAsync(response, "code");

    private static Task<string> ErrorMessageAsync(HttpResponseMessage response) => ErrorFieldAsync(response, "message");

    private static async Task<string> ErrorFieldAsync(HttpResponseMessage response, string field)
    {
        await response.Content.LoadIntoBufferAsync();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("error").GetProperty(field).GetString()!;
    }
}
