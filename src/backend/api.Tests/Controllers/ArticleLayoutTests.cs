using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Articles.DTOs;
using SharpQuill.Core.Articles;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers;

[Collection("Database")]
public class ArticleLayoutTests
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public ArticleLayoutTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        AuthHelper.CreateAuthenticatedClient(_client);
    }

    private static object CreateLayoutPayload() => new
    {
        Sections = new[]
        {
            new
            {
                Type = "full",
                Blocks = new object[]
                {
                    new { Type = "text", Content = "# Hello\nThis is a layout article." }
                }
            },
            new
            {
                Type = "two-column",
                Blocks = new object[]
                {
                    new { Type = "text", Content = "Left column content." },
                    new { Type = "text", Content = "Right column content." }
                }
            }
        }
    };

    [Fact]
    public async Task CreateArticle_WithLayout_StoresAndReturnsLayout()
    {
        var uniqueSlug = $"layout-create-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Layout Article",
            Slug = uniqueSlug,
            Layout = CreateLayoutPayload(),
            Excerpt = "Layout excerpt",
            Status = ArticleStatus.Draft
        };

        var response = await _client.PostAsJsonAsync("/api/admin/articles", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        result.Should().NotBeNull();
        result!.Data.Layout.Should().NotBeNull();
        result.Data.Layout!.Sections.Should().HaveCount(2);
        result.Data.Layout.Sections[0].Type.Should().Be("full");
        result.Data.Layout.Sections[0].Blocks.Should().HaveCount(1);
        result.Data.Layout.Sections[0].Blocks[0].Type.Should().Be("text");
        result.Data.Layout.Sections[1].Type.Should().Be("two-column");
        result.Data.Layout.Sections[1].Blocks.Should().HaveCount(2);

        // Content should be flattened from layout text blocks
        result.Data.Content.Should().NotBeNullOrEmpty();
        result.Data.Content.Should().Contain("Hello");
        result.Data.Content.Should().Contain("Left column");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{result.Data.Id}");
    }

    [Fact]
    public async Task CreateArticle_WithoutLayout_LayoutIsNull()
    {
        var uniqueSlug = $"no-layout-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Simple Article",
            Slug = uniqueSlug,
            Content = "Simple markdown content",
            Status = ArticleStatus.Draft
        };

        var response = await _client.PostAsJsonAsync("/api/admin/articles", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        result.Should().NotBeNull();
        result!.Data.Layout.Should().BeNull();
        result.Data.Content.Should().Be("Simple markdown content");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{result.Data.Id}");
    }

    [Fact]
    public async Task UpdateArticle_WithLayout_CreatesNewVersion()
    {
        var uniqueSlug = $"layout-update-{Guid.NewGuid():N}";

        // Create without layout
        var createDto = new
        {
            Title = "Initially Simple",
            Slug = uniqueSlug,
            Content = "Simple content",
            Status = ArticleStatus.Draft
        };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;
        created.Data.TotalVersions.Should().Be(1);

        // Update to layout mode
        var updateDto = new
        {
            Title = "Now With Layout",
            Slug = uniqueSlug,
            Layout = CreateLayoutPayload(),
            Status = ArticleStatus.Draft
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/admin/articles/{articleId}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();

        updated!.Data.TotalVersions.Should().Be(2);
        updated.Data.Layout.Should().NotBeNull();
        updated.Data.Layout!.Sections.Should().HaveCount(2);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
    }

    [Fact]
    public async Task PublishedLayoutArticle_PublicApiReturnsComposedHtml()
    {
        var uniqueSlug = $"layout-public-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Public Layout Article",
            Slug = uniqueSlug,
            Layout = new
            {
                Sections = new[]
                {
                    new
                    {
                        Type = "full",
                        Blocks = new object[]
                        {
                            new { Type = "text", Content = "**Bold text** in layout." }
                        }
                    }
                }
            },
            Excerpt = "Layout excerpt",
            Status = ArticleStatus.Published
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;

        // Fetch via public API
        var publicClient = _fixture.Factory.CreateClient();
        var publicResponse = await publicClient.GetAsync($"/api/v1/articles/{uniqueSlug}");

        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Parse as raw JSON to check the layout field
        var json = await publicResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        // Layout should be present with rendered HTML
        data.TryGetProperty("layout", out var layoutProp).Should().BeTrue();
        layoutProp.ValueKind.Should().NotBe(JsonValueKind.Null);

        var sections = layoutProp.GetProperty("sections");
        sections.GetArrayLength().Should().Be(1);

        var firstBlock = sections[0].GetProperty("blocks")[0];
        firstBlock.GetProperty("type").GetString().Should().Be("text");
        var html = firstBlock.GetProperty("html").GetString();
        html.Should().Contain("<strong>Bold text</strong>");

        // Content should be flattened markdown (fallback)
        data.GetProperty("content").GetString().Should().Contain("Bold text");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
    }

    [Fact]
    public async Task SimpleArticle_PublicApiReturnsNullLayout()
    {
        // Verify existing simple articles still work
        var publicClient = _fixture.Factory.CreateClient();
        var response = await publicClient.GetAsync($"/api/v1/articles/{TestSeedData.PublishedArticle1Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Data.Layout.Should().BeNull();
        result.Data.Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task VersionDetail_IncludesLayoutData()
    {
        var uniqueSlug = $"layout-version-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Layout Version Test",
            Slug = uniqueSlug,
            Layout = CreateLayoutPayload(),
            Status = ArticleStatus.Draft
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;

        // Get versions
        var versionsResponse = await _client.GetAsync($"/api/admin/articles/{articleId}/versions");
        var versions = await versionsResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ArticleVersionDto>>>();
        var versionId = versions!.Data[0].Id;

        // Get version detail
        var detailResponse = await _client.GetAsync($"/api/admin/articles/{articleId}/versions/{versionId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleVersionDetailDto>>();
        detail.Should().NotBeNull();
        detail!.Data.Layout.Should().NotBeNull();
        detail.Data.Layout!.Sections.Should().HaveCount(2);

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
    }

    [Fact]
    public async Task CreateArticle_WithInvalidLayout_ReturnsBadRequest()
    {
        var uniqueSlug = $"layout-invalid-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Invalid Layout Article",
            Slug = uniqueSlug,
            Layout = new
            {
                Sections = new[]
                {
                    new
                    {
                        Type = "invalid-type",
                        Blocks = new object[]
                        {
                            new { Type = "text", Content = "Some content" }
                        }
                    }
                }
            },
            Status = ArticleStatus.Draft
        };

        var response = await _client.PostAsJsonAsync("/api/admin/articles", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateArticle_WithEmptySections_ReturnsBadRequest()
    {
        var uniqueSlug = $"layout-empty-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Empty Layout Article",
            Slug = uniqueSlug,
            Layout = new
            {
                Sections = Array.Empty<object>()
            },
            Status = ArticleStatus.Draft
        };

        var response = await _client.PostAsJsonAsync("/api/admin/articles", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateArticle_WithNestedContainer_StoresAndReturnsNestedLayout()
    {
        var uniqueSlug = $"layout-nested-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Nested Layout Article",
            Slug = uniqueSlug,
            Layout = new
            {
                Sections = new[]
                {
                    new
                    {
                        Type = "full",
                        Blocks = new object[]
                        {
                            new { Type = "text", Content = "Top-level text." },
                            new
                            {
                                Type = "container",
                                Sections = new[]
                                {
                                    new
                                    {
                                        Type = "two-column",
                                        Blocks = new object[]
                                        {
                                            new { Type = "text", Content = "Nested left column." },
                                            new { Type = "text", Content = "Nested right column." }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            Excerpt = "Nested layout excerpt",
            Status = ArticleStatus.Draft
        };

        var response = await _client.PostAsJsonAsync("/api/admin/articles", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        result.Should().NotBeNull();
        result!.Data.Layout.Should().NotBeNull();
        result.Data.Layout!.Sections.Should().HaveCount(1);
        result.Data.Layout.Sections[0].Blocks.Should().HaveCount(2);
        result.Data.Layout.Sections[0].Blocks[0].Type.Should().Be("text");
        result.Data.Layout.Sections[0].Blocks[1].Type.Should().Be("container");
        result.Data.Layout.Sections[0].Blocks[1].Sections.Should().NotBeNull();
        result.Data.Layout.Sections[0].Blocks[1].Sections!.Should().HaveCount(1);
        result.Data.Layout.Sections[0].Blocks[1].Sections![0].Type.Should().Be("two-column");
        result.Data.Layout.Sections[0].Blocks[1].Sections![0].Blocks.Should().HaveCount(2);

        // Flattened content should include all text blocks
        result.Data.Content.Should().Contain("Top-level text");
        result.Data.Content.Should().Contain("Nested left column");
        result.Data.Content.Should().Contain("Nested right column");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{result.Data.Id}");
    }

    [Fact]
    public async Task PublishedNestedLayoutArticle_PublicApiRendersNestedHtml()
    {
        var uniqueSlug = $"layout-nested-public-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Public Nested Layout",
            Slug = uniqueSlug,
            Layout = new
            {
                Sections = new[]
                {
                    new
                    {
                        Type = "full",
                        Blocks = new object[]
                        {
                            new
                            {
                                Type = "container",
                                Sections = new[]
                                {
                                    new
                                    {
                                        Type = "full",
                                        Blocks = new object[]
                                        {
                                            new { Type = "text", Content = "**Nested bold** content." }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            Excerpt = "Nested public excerpt",
            Status = ArticleStatus.Published
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;

        // Fetch via public API
        var publicClient = _fixture.Factory.CreateClient();
        var publicResponse = await publicClient.GetAsync($"/api/v1/articles/{uniqueSlug}");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await publicResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        // Navigate into nested container
        var layout = data.GetProperty("layout");
        var sections = layout.GetProperty("sections");
        sections.GetArrayLength().Should().Be(1);

        var containerBlock = sections[0].GetProperty("blocks")[0];
        containerBlock.GetProperty("type").GetString().Should().Be("container");

        var nestedSections = containerBlock.GetProperty("sections");
        nestedSections.GetArrayLength().Should().Be(1);

        var nestedBlock = nestedSections[0].GetProperty("blocks")[0];
        nestedBlock.GetProperty("type").GetString().Should().Be("text");
        var html = nestedBlock.GetProperty("html").GetString();
        html.Should().Contain("<strong>Nested bold</strong>");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
    }

    [Fact]
    public async Task CreateArticle_WithExcessiveNesting_ReturnsBadRequest()
    {
        var uniqueSlug = $"layout-deep-{Guid.NewGuid():N}";
        // Build 4 levels deep: section > container > section > container > section > container > section > text
        var createDto = new
        {
            Title = "Deep Nesting Article",
            Slug = uniqueSlug,
            Layout = new
            {
                Sections = new[]
                {
                    new
                    {
                        Type = "full",
                        Blocks = new object[]
                        {
                            new
                            {
                                Type = "container",
                                Sections = new[]
                                {
                                    new
                                    {
                                        Type = "full",
                                        Blocks = new object[]
                                        {
                                            new
                                            {
                                                Type = "container",
                                                Sections = new[]
                                                {
                                                    new
                                                    {
                                                        Type = "full",
                                                        Blocks = new object[]
                                                        {
                                                            new
                                                            {
                                                                Type = "container",
                                                                Sections = new[]
                                                                {
                                                                    new
                                                                    {
                                                                        Type = "full",
                                                                        Blocks = new object[]
                                                                        {
                                                                            new { Type = "text", Content = "Too deep." }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            Status = ArticleStatus.Draft
        };

        var response = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateArticle_WithCodeBlock_StoresAndReturnsCorrectly()
    {
        var uniqueSlug = $"layout-code-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Code Block Article",
            Slug = uniqueSlug,
            Layout = new
            {
                Sections = new[]
                {
                    new
                    {
                        Type = "full",
                        Blocks = new object[]
                        {
                            new { Type = "code", Content = "console.log('hello');", Language = "javascript" }
                        }
                    }
                }
            },
            Status = ArticleStatus.Draft
        };

        var response = await _client.PostAsJsonAsync("/api/admin/articles", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        result.Should().NotBeNull();
        result!.Data.Layout.Should().NotBeNull();
        result.Data.Layout!.Sections.Should().HaveCount(1);
        result.Data.Layout.Sections[0].Blocks.Should().HaveCount(1);
        result.Data.Layout.Sections[0].Blocks[0].Type.Should().Be("code");
        result.Data.Layout.Sections[0].Blocks[0].Content.Should().Be("console.log('hello');");
        result.Data.Layout.Sections[0].Blocks[0].Language.Should().Be("javascript");

        // Flattened content should contain the code as a fenced block
        result.Data.Content.Should().Contain("console.log('hello');");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{result.Data.Id}");
    }

    [Fact]
    public async Task PublishedCodeBlockArticle_PublicApiReturnsRenderedHtml()
    {
        var uniqueSlug = $"layout-code-public-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Public Code Block Article",
            Slug = uniqueSlug,
            Layout = new
            {
                Sections = new[]
                {
                    new
                    {
                        Type = "full",
                        Blocks = new object[]
                        {
                            new { Type = "code", Content = "<div>Hello</div>", Language = "html" }
                        }
                    }
                }
            },
            Excerpt = "Code block excerpt",
            Status = ArticleStatus.Published
        };

        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();
        var articleId = created!.Data.Id;

        // Fetch via public API
        var publicClient = _fixture.Factory.CreateClient();
        var publicResponse = await publicClient.GetAsync($"/api/v1/articles/{uniqueSlug}");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await publicResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        var layout = data.GetProperty("layout");
        var firstBlock = layout.GetProperty("sections")[0].GetProperty("blocks")[0];
        firstBlock.GetProperty("type").GetString().Should().Be("code");

        var html = firstBlock.GetProperty("html").GetString();
        html.Should().Contain("<pre><code");
        html.Should().Contain("language-html");
        // Content should be HTML-escaped
        html.Should().Contain("&lt;div&gt;Hello&lt;/div&gt;");

        firstBlock.GetProperty("language").GetString().Should().Be("html");

        // Cleanup
        await _client.DeleteAsync($"/api/admin/articles/{articleId}");
    }

    [Fact]
    public async Task CreateArticle_WithCodeBlockEmptyContent_ReturnsBadRequest()
    {
        var uniqueSlug = $"layout-code-empty-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Empty Code Block Article",
            Slug = uniqueSlug,
            Layout = new
            {
                Sections = new[]
                {
                    new
                    {
                        Type = "full",
                        Blocks = new object[]
                        {
                            new { Type = "code", Content = "", Language = "javascript" }
                        }
                    }
                }
            },
            Status = ArticleStatus.Draft
        };

        var response = await _client.PostAsJsonAsync("/api/admin/articles", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateArticle_ContainerWithoutSections_ReturnsBadRequest()
    {
        var uniqueSlug = $"layout-container-empty-{Guid.NewGuid():N}";
        var createDto = new
        {
            Title = "Empty Container Article",
            Slug = uniqueSlug,
            Layout = new
            {
                Sections = new[]
                {
                    new
                    {
                        Type = "full",
                        Blocks = new object[]
                        {
                            new { Type = "container" }
                        }
                    }
                }
            },
            Status = ArticleStatus.Draft
        };

        var response = await _client.PostAsJsonAsync("/api/admin/articles", createDto);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static object SingleBlockLayout(object block) => new
    {
        Sections = new[] { new { Type = "full", Blocks = new[] { block } } }
    };

    private async Task<HttpResponseMessage> CreateDraftWithLayout(object layout) =>
        await _client.PostAsJsonAsync("/api/admin/articles", new
        {
            Title = "Registry Layout Article",
            Slug = $"layout-registry-{Guid.NewGuid():N}",
            Layout = layout,
            Status = ArticleStatus.Draft
        });

    [Theory]
    [InlineData("hologram")]
    [InlineData("Text")]
    [InlineData("__proto__")]
    [InlineData("")]
    public async Task CreateArticle_WithUnknownBlockType_ReturnsBadRequest(string type)
    {
        var response = await CreateDraftWithLayout(SingleBlockLayout(new { Type = type, Content = "Some content" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("contactform")]
    [InlineData("")]
    public async Task CreateArticle_WithUnknownComponentName_ReturnsBadRequest(string componentName)
    {
        var response = await CreateDraftWithLayout(SingleBlockLayout(new { Type = "component", ComponentName = componentName }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateArticle_WithUnknownBlockType_ReturnsBadRequest()
    {
        var createResponse = await CreateDraftWithLayout(CreateLayoutPayload());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>();

        var response = await _client.PutAsJsonAsync($"/api/admin/articles/{created!.Data.Id}", new
        {
            Title = "Registry Layout Article",
            Slug = created.Data.Slug,
            Layout = SingleBlockLayout(new { Type = "hologram", Content = "x" }),
            Status = ArticleStatus.Draft
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await _client.DeleteAsync($"/api/admin/articles/{created.Data.Id}");
    }

    // Every block type the registry knows, in one layout: the admin API returns it as stored and the public API
    // renders each block. Guards the move of the per-type rules into LayoutBlockTypes.
    [Fact]
    public async Task PublishedLayout_WithEveryBlockType_RoundTripsAndRenders()
    {
        var slug = $"layout-every-block-{Guid.NewGuid():N}";
        var blocks = new object[]
        {
            new { Type = "text", Content = "**Text** block" },
            new { Type = "image", MediaId = int.MaxValue, Alt = "Missing image", Caption = "A *caption*" },
            new { Type = "code", Content = "var x = 1 < 2;", Language = "csharp" },
            new { Type = "link", Url = "https://example.com/", Title = "Example" },
            new { Type = "tweet", Url = "https://x.com/user/status/1", Title = "User", Description = "Tweet text" },
            new
            {
                Type = "container",
                Sections = new[] { new { Type = "full", Blocks = new object[] { new { Type = "text", Content = "Nested" } } } }
            },
            new
            {
                Type = "cards", Heading = "Cards *heading*", Content = "Intro",
                Items = new[] { new { Icon = "rocket", Title = "Card", Description = "Card *text*" } }
            },
            new { Type = "steps", Heading = "Steps", Items = new[] { new { Number = "01", Title = "Step", Description = "Do it" } } },
            new { Type = "cta", Title = "Call", Description = "To *action*", ButtonText = "Go", ButtonUrl = "/contact" },
            new { Type = "component", ComponentName = "ContactForm" }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/articles", new
        {
            Title = "Every Block",
            Slug = slug,
            Layout = new { Sections = new[] { new { Type = "full", Style = "tinted", Blocks = blocks } } },
            Status = ArticleStatus.Published
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<ArticleAdminDto>>())!.Data;

        var stored = created.Layout!.Sections.Single();
        stored.Style.Should().Be("tinted");
        stored.Blocks.Select(b => b.Type).Should().Equal(
            "text", "image", "code", "link", "tweet", "container", "cards", "steps", "cta", "component");
        stored.Blocks[9].ComponentName.Should().Be("ContactForm");
        created.Content.Should().Contain("**Text** block").And.Contain("```csharp").And.Contain("[Example](https://example.com/)")
            .And.Contain("[Tweet](https://x.com/user/status/1)").And.Contain("Nested").And.Contain("- **Card**: Card *text*")
            .And.Contain("01. **Step**: Do it").And.Contain("## Call");

        var publicResponse = await _fixture.Factory.CreateClient().GetAsync($"/api/v1/articles/{slug}");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = JsonDocument.Parse(await publicResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        var section = data.GetProperty("layout").GetProperty("sections")[0];
        section.GetProperty("style").GetString().Should().Be("tinted");
        var rendered = section.GetProperty("blocks");

        rendered[0].GetProperty("html").GetString().Should().Contain("<strong>Text</strong>");
        rendered[1].GetProperty("imageUrl").ValueKind.Should().Be(JsonValueKind.Null);
        rendered[1].GetProperty("captionHtml").GetString().Should().Be("A <em>caption</em>");
        rendered[2].GetProperty("html").GetString().Should().Be("<pre><code class=\"language-csharp\">var x = 1 &lt; 2;</code></pre>");
        rendered[3].GetProperty("url").GetString().Should().Be("https://example.com/");
        rendered[4].GetProperty("description").GetString().Should().Be("Tweet text");
        rendered[5].GetProperty("sections")[0].GetProperty("blocks")[0].GetProperty("html").GetString().Should().Contain("Nested");
        rendered[6].GetProperty("headingHtml").GetString().Should().Be("Cards <em>heading</em>");
        rendered[6].GetProperty("items")[0].GetProperty("descriptionHtml").GetString().Should().Contain("<em>text</em>");
        rendered[7].GetProperty("items")[0].GetProperty("number").GetString().Should().Be("01");
        rendered[8].GetProperty("buttonUrl").GetString().Should().Be("/contact");
        rendered[8].GetProperty("titleHtml").GetString().Should().Be("Call");
        rendered[9].GetProperty("componentName").GetString().Should().Be("ContactForm");

        await _client.DeleteAsync($"/api/admin/articles/{created.Id}");
    }
}
