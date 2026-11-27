namespace SharpQuill.Api.Tests.Fixtures;

public static class TestSeedData
{
    // Author constants
    public const int AdminAuthorId = 1;
    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "admin123";
    public const int CertAuthorId = 2;
    public const string CertAuthorEmail = "cert@example.com";
    public const string CertThumbprint = "ABC123DEF456TEST";

    // Category constants
    public const int UncategorizedId = 1;
    public const string UncategorizedSlug = "uncategorized";
    public const int TechnologyCategoryId = 2;
    public const string TechnologySlug = "technology";
    public const int LifestyleCategoryId = 3;
    public const string LifestyleSlug = "lifestyle";

    // Tag constants
    public const int CSharpTagId = 1;
    public const string CSharpTagSlug = "csharp";
    public const int DotNetTagId = 2;
    public const string DotNetTagSlug = "dotnet";
    public const int TutorialTagId = 3;
    public const string TutorialTagSlug = "tutorial";

    // Series constants
    public const int GettingStartedSeriesId = 1;
    public const string GettingStartedSeriesSlug = "getting-started";
    public const int AdvancedTopicsSeriesId = 2;
    public const string AdvancedTopicsSeriesSlug = "advanced-topics";

    // Article constants
    public const int PublishedArticle1Id = 1;
    public const string PublishedArticle1Slug = "intro-to-csharp";
    public const int PublishedArticle2Id = 2;
    public const string PublishedArticle2Slug = "advanced-csharp";
    public const int PublishedArticle3Id = 3;
    public const string PublishedArticle3Slug = "lifestyle-tips";
    public const int DraftArticleId = 4;
    public const string DraftArticleSlug = "draft-article";
    public const int ArchivedArticleId = 5;
    public const string ArchivedArticleSlug = "archived-article";
    public const int GatedArticleId = 6;
    public const string GatedArticleSlug = "gated-article";

    // Website user constants
    public const int ApprovedWebsiteUserId = 1;
    public const string ApprovedWebsiteUserEmail = "approved@example.com";
    public const string ApprovedWebsiteUserName = "Approved User";
    public const int PendingWebsiteUserId = 2;
    public const string PendingWebsiteUserEmail = "pending@example.com";
    public const int RejectedWebsiteUserId = 3;
    public const string RejectedWebsiteUserEmail = "rejected@example.com";

    // Website login token constants
    public const string ValidLoginToken = "valid_token_abc123";
    public const string UsedLoginToken = "used_token_def456";
    public const string ExpiredLoginToken = "expired_token_ghi789";

    // ArticleContent (version) constants
    public const int Article1Version1Id = 1;
    public const int Article2Version1Id = 2;
    public const int Article3Version1Id = 3;
    public const int Article4Version1Id = 4;
    public const int Article5Version1Id = 5;

    // Page constants (pages are articles with content_type = 1)
    public const int HjemPageId = 100;
    public const string HjemPageSlug = "hjem";
    public const int OmPageId = 101;
    public const string OmPageSlug = "om";

    // Navigation constants
    public const int MainMenuId = 1;
    public const string MainMenuSlug = "main-menu";
    public const int FooterLinksId = 2;
    public const string FooterLinksSlug = "footer-links";

    public const string SeedSql = @"
        -- Authors
        INSERT INTO authors (id, email, password_hash, name, bio, avatar_url, certificate_thumbprint, created_at)
        VALUES
            (1, 'admin@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.VTtYE9rikCKK5.', 'Admin', 'Site administrator', NULL, NULL, '2024-01-01T00:00:00Z'),
            (2, 'cert@example.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.VTtYE9rikCKK5.', 'CertUser', 'Certificate auth user', NULL, 'ABC123DEF456TEST', '2024-01-01T00:00:00Z');

        -- Categories
        INSERT INTO categories (id, name, slug, description, created_at)
        VALUES
            (1, 'Uncategorized', 'uncategorized', 'Default category', '2024-01-01T00:00:00Z'),
            (2, 'Technology', 'technology', 'Tech articles', '2024-01-01T00:00:00Z'),
            (3, 'Lifestyle', 'lifestyle', 'Lifestyle articles', '2024-01-01T00:00:00Z');

        -- Tags
        INSERT INTO tags (id, name, slug, created_at)
        VALUES
            (1, 'C#', 'csharp', '2024-01-01T00:00:00Z'),
            (2, '.NET', 'dotnet', '2024-01-01T00:00:00Z'),
            (3, 'Tutorial', 'tutorial', '2024-01-01T00:00:00Z');

        -- Series
        INSERT INTO series (id, name, slug, description, created_at)
        VALUES
            (1, 'Getting Started', 'getting-started', 'Beginner tutorials', '2024-01-01T00:00:00Z'),
            (2, 'Advanced Topics', 'advanced-topics', 'Advanced concepts', '2024-01-01T00:00:00Z');

        -- Articles (insert first without published_version_id due to FK ordering)
        -- content_type: 0 = Article, 1 = Page
        INSERT INTO articles (id, title, slug, content_type, author_id, category_id, series_id, series_order, status, requires_sign_in, published_at, created_at)
        VALUES
            (1, 'Intro to C#', 'intro-to-csharp', 0, 1, 2, 1, 1, 1, false, '2024-06-01T12:00:00Z', '2024-06-01T10:00:00Z'),
            (2, 'Advanced C#', 'advanced-csharp', 0, 1, 2, 1, 2, 1, false, '2024-06-02T12:00:00Z', '2024-06-02T10:00:00Z'),
            (3, 'Lifestyle Tips', 'lifestyle-tips', 0, 1, 3, NULL, NULL, 1, false, '2024-06-03T12:00:00Z', '2024-06-03T10:00:00Z'),
            (4, 'Draft Article', 'draft-article', 0, 1, 1, NULL, NULL, 0, false, NULL, '2024-06-04T10:00:00Z'),
            (5, 'Archived Article', 'archived-article', 0, 1, 1, NULL, NULL, 2, false, '2024-01-01T12:00:00Z', '2024-01-01T10:00:00Z'),
            (6, 'Gated Article', 'gated-article', 0, 1, 2, NULL, NULL, 1, true, '2024-06-05T12:00:00Z', '2024-06-05T10:00:00Z'),
            -- Pages (content_type = 1, no author)
            (100, 'Hjem', 'hjem', 1, NULL, NULL, NULL, NULL, 1, false, '2024-01-01T00:00:00Z', '2024-01-01T00:00:00Z'),
            (101, 'Om os', 'om', 1, NULL, NULL, NULL, NULL, 1, false, '2024-01-01T00:00:00Z', '2024-01-01T00:00:00Z');

        -- Article content versions
        INSERT INTO article_contents (id, article_id, content, excerpt, meta_title, meta_description, version_number, created_at)
        VALUES
            (1, 1, '# Intro\nLearn C# basics.', 'Learn C# basics', NULL, NULL, 1, '2024-06-01T10:00:00Z'),
            (2, 2, '# Advanced\nAdvanced C# topics.', 'Advanced C# topics', NULL, NULL, 1, '2024-06-02T10:00:00Z'),
            (3, 3, '# Tips\nSome lifestyle tips.', 'Lifestyle tips', NULL, NULL, 1, '2024-06-03T10:00:00Z'),
            (4, 4, '# Draft\nWork in progress.', 'Draft excerpt', NULL, NULL, 1, '2024-06-04T10:00:00Z'),
            (5, 5, '# Archived\nOld content.', 'Archived excerpt', NULL, NULL, 1, '2024-01-01T10:00:00Z'),
            (6, 6, '# Gated\nExclusive content.', 'Gated excerpt', NULL, NULL, 1, '2024-06-05T10:00:00Z'),
            -- Page content versions
            (100, 100, '# Velkommen', 'Velkommen', 'Forside', 'Udforsk artikler', 1, '2024-01-01T00:00:00Z'),
            (101, 101, '# Om os\nVelkommen til vores platform.', 'Om os', 'Om os', 'Om vores platform', 1, '2024-01-01T00:00:00Z');

        -- Set published_version_id for published/archived articles and pages
        UPDATE articles SET published_version_id = 1 WHERE id = 1;
        UPDATE articles SET published_version_id = 2 WHERE id = 2;
        UPDATE articles SET published_version_id = 3 WHERE id = 3;
        UPDATE articles SET published_version_id = 5 WHERE id = 5;
        UPDATE articles SET published_version_id = 6 WHERE id = 6;
        UPDATE articles SET published_version_id = 100 WHERE id = 100;
        UPDATE articles SET published_version_id = 101 WHERE id = 101;

        -- Website Users
        INSERT INTO website_users (id, name, email, status, created_at, updated_at)
        VALUES
            (1, 'Approved User', 'approved@example.com', 1, '2024-01-01T00:00:00Z', '2024-01-02T00:00:00Z'),
            (2, 'Pending User', 'pending@example.com', 0, '2024-01-01T00:00:00Z', NULL),
            (3, 'Rejected User', 'rejected@example.com', 2, '2024-01-01T00:00:00Z', '2024-01-03T00:00:00Z');

        -- Website Login Tokens
        INSERT INTO website_login_tokens (id, website_user_id, token, expires_at, created_at, used_at)
        VALUES
            (1, 1, encode(sha256('valid_token_abc123'::bytea), 'hex'), '2099-12-31T23:59:59Z', '2024-01-01T00:00:00Z', NULL),
            (2, 1, encode(sha256('used_token_def456'::bytea), 'hex'), '2099-12-31T23:59:59Z', '2024-01-01T00:00:00Z', '2024-01-02T00:00:00Z'),
            (3, 1, encode(sha256('expired_token_ghi789'::bytea), 'hex'), '2020-01-01T00:00:00Z', '2024-01-01T00:00:00Z', NULL);

        -- Article Tags
        INSERT INTO article_tags (article_id, tag_id)
        VALUES
            (1, 1), (1, 2), (1, 3),
            (2, 1), (2, 2),
            (3, 3);

        -- Navigations
        INSERT INTO navigations (id, name, slug)
        VALUES
            (1, 'Hovedmenu', 'main-menu'),
            (2, 'Footer-links', 'footer-links');

        -- Navigation Items (main menu) - article_id references pages (content_type=1)
        INSERT INTO navigation_items (id, navigation_id, label, item_type, system_route, article_id, url, parent_id, position, open_in_new_tab)
        VALUES
            (1, 1, 'Hjem', 1, '/', NULL, NULL, NULL, 1, false),
            (2, 1, 'Blog', 1, '/blog', NULL, NULL, NULL, 2, false),
            (3, 1, 'Om', 0, NULL, 101, NULL, NULL, 3, false),
            -- Footer items
            (4, 2, 'Hjem', 1, '/', NULL, NULL, NULL, 1, false),
            (5, 2, 'Blog', 1, '/blog', NULL, NULL, NULL, 2, false);

        -- Site Settings
        INSERT INTO site_settings (id, key, value)
        VALUES
            (1, 'site_name', 'Test CMS'),
            (2, 'site_description', 'A test CMS site'),
            (3, 'site_url', 'https://test.example.com'),
            (4, 'contact_email', 'test@example.com');
    ";
}
