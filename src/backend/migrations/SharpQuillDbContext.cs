using SharpQuill.Core.Articles;
using SharpQuill.Core.Auth;
using SharpQuill.Core.Authors;
using SharpQuill.Core.Categories;
using SharpQuill.Core.Comments;
using SharpQuill.Core.Likes;
using SharpQuill.Core.Navigations;
using SharpQuill.Core.Settings;
using SharpQuill.Core.Media;
using SharpQuill.Core.Series;
using SharpQuill.Core.Tags;
using SharpQuill.Core.WebsiteUsers;
using Microsoft.EntityFrameworkCore;

namespace SharpQuill.Migrations;

public class SharpQuillDbContext : DbContext
{
    public SharpQuillDbContext(DbContextOptions<SharpQuillDbContext> options) : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleContent> ArticleContents => Set<ArticleContent>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<ArticleSeries> Series => Set<ArticleSeries>();
    public DbSet<MediaItem> Media => Set<MediaItem>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Navigation> Navigations => Set<Navigation>();
    public DbSet<NavigationItem> NavigationItems => Set<NavigationItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ArticleLike> ArticleLikes => Set<ArticleLike>();
    public DbSet<WebsiteUser> WebsiteUsers => Set<WebsiteUser>();
    public DbSet<WebsiteLoginToken> WebsiteLoginTokens => Set<WebsiteLoginToken>();
    public DbSet<WebsiteRefreshToken> WebsiteRefreshTokens => Set<WebsiteRefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply snake_case naming convention for all tables and columns
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Table names: snake_case, plural
            entity.SetTableName(ToSnakeCase(entity.GetTableName()!));

            // Column names: snake_case
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            // Foreign key names: snake_case
            foreach (var key in entity.GetForeignKeys())
            {
                key.SetConstraintName(ToSnakeCase(key.GetConstraintName()!));
            }

            // Index names: snake_case
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }
        }

        // Author configuration
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CertificateThumbprint).HasMaxLength(128);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20).HasDefaultValue("admin");
            entity.HasIndex(e => e.CertificateThumbprint).IsUnique().HasFilter("certificate_thumbprint IS NOT NULL");
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // Series configuration
        modelBuilder.Entity<ArticleSeries>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.ThemeSlug).HasMaxLength(50);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // Article configuration
        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AuthorName).HasMaxLength(200);
            entity.Property(e => e.Status).HasDefaultValue(ArticleStatus.Draft);
            entity.Property(e => e.ContentType).HasDefaultValue(ContentType.Article);

            // Transport properties - not mapped to DB columns
            entity.Ignore(e => e.Content);
            entity.Ignore(e => e.Layout);
            entity.Ignore(e => e.Excerpt);
            entity.Ignore(e => e.MetaTitle);
            entity.Ignore(e => e.MetaDescription);
            entity.Ignore(e => e.PageStyle);
            entity.Ignore(e => e.FeaturedImageSize);
            entity.Ignore(e => e.CurrentVersionNumber);
            entity.Ignore(e => e.TotalVersions);

            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => e.SeriesId);
            entity.Property(e => e.CommentsEnabled).HasDefaultValue(true);
            entity.Property(e => e.RequiresSignIn).HasDefaultValue(false);
            entity.HasIndex(e => e.ContentType);

            entity.HasOne(e => e.Author)
                .WithMany(a => a.Articles)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Articles)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Series)
                .WithMany(s => s.Articles)
                .HasForeignKey(e => e.SeriesId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.FeaturedImage)
                .WithMany()
                .HasForeignKey(e => e.FeaturedImageId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.PublishedVersion)
                .WithMany()
                .HasForeignKey(e => e.PublishedVersionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ArticleContent configuration
        modelBuilder.Entity<ArticleContent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.Excerpt).HasMaxLength(500);
            entity.Property(e => e.MetaTitle).HasMaxLength(70);
            entity.Property(e => e.MetaDescription).HasMaxLength(160);
            entity.Property(e => e.Layout).HasColumnType("jsonb");
            entity.Property(e => e.FeaturedImageSize).HasMaxLength(20);
            entity.Property(e => e.VersionNumber).IsRequired();

            entity.HasIndex(e => new { e.ArticleId, e.VersionNumber }).IsUnique();

            entity.HasOne(e => e.Article)
                .WithMany(a => a.Versions)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ArticleTag (join table) configuration
        modelBuilder.Entity<ArticleTag>(entity =>
        {
            entity.HasKey(e => new { e.ArticleId, e.TagId });

            entity.HasOne(e => e.Article)
                .WithMany(a => a.ArticleTags)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                .WithMany(t => t.ArticleTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Media configuration
        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Filename).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OriginalFilename).IsRequired().HasMaxLength(255);
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AltText).HasMaxLength(255);
            entity.Property(e => e.ThumbnailPath).HasMaxLength(500);
            entity.Property(e => e.MediumPath).HasMaxLength(500);

            entity.HasOne(e => e.Author)
                .WithMany(a => a.Media)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SiteSettings configuration
        modelBuilder.Entity<SiteSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ReplacedByToken).HasMaxLength(500);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.AuthorId);

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.IsExpired);
            entity.Ignore(e => e.IsRevoked);
            entity.Ignore(e => e.IsActive);
        });

        // Navigation configuration
        modelBuilder.Entity<Navigation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // NavigationItem configuration
        modelBuilder.Entity<NavigationItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SystemRoute).HasMaxLength(200);
            entity.Property(e => e.Url).HasMaxLength(500);

            entity.HasOne(e => e.Navigation)
                .WithMany(n => n.Items)
                .HasForeignKey(e => e.NavigationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Article)
                .WithMany()
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.NavigationId);
        });

        // Comment configuration
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AuthorEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Content).IsRequired().HasColumnType("text");
            entity.Property(e => e.Status).HasDefaultValue(CommentStatus.Pending);
            entity.Property(e => e.IpAddress).HasMaxLength(45);

            entity.HasIndex(e => e.ArticleId);
            entity.HasIndex(e => e.ParentCommentId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.Ignore(e => e.ArticleTitle);

            entity.HasOne<Article>()
                .WithMany()
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Replies)
                .HasForeignKey(e => e.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ArticleLike configuration
        modelBuilder.Entity<ArticleLike>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VisitorId).IsRequired().HasMaxLength(36);
            entity.Property(e => e.IpAddress).HasMaxLength(45);

            entity.HasIndex(e => new { e.ArticleId, e.VisitorId }).IsUnique();

            entity.HasOne<Article>()
                .WithMany()
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // WebsiteUser configuration
        modelBuilder.Entity<WebsiteUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).HasDefaultValue(WebsiteUserStatus.Pending);

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        // WebsiteLoginToken configuration
        modelBuilder.Entity<WebsiteLoginToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);

            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.WebsiteUser)
                .WithMany()
                .HasForeignKey(e => e.WebsiteUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.IsValid);
        });

        // WebsiteRefreshToken configuration
        modelBuilder.Entity<WebsiteRefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ReplacedByToken).HasMaxLength(500);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.WebsiteUserId);

            entity.HasOne(e => e.WebsiteUser)
                .WithMany()
                .HasForeignKey(e => e.WebsiteUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.IsExpired);
            entity.Ignore(e => e.IsRevoked);
            entity.Ignore(e => e.IsActive);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // No author is seeded: the first admin is created by db-init (see db-init/Program.cs)

        // Seed default category
        modelBuilder.Entity<Category>().HasData(new Category
        {
            Id = 1,
            Name = "Uncategorized",
            Slug = "uncategorized",
            Description = "Default category for articles",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed pages (as articles with ContentType.Page)
        // Note: seed IDs start high to avoid conflicts with real articles
        modelBuilder.Entity<Article>().HasData(
            new Article
            {
                Id = 1000,
                Title = "Hjem",
                Slug = "hjem",
                Status = ArticleStatus.Published,
                ContentType = ContentType.Page,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Article
            {
                Id = 1001,
                Title = "Om os",
                Slug = "om",
                Status = ArticleStatus.Published,
                ContentType = ContentType.Page,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed page content versions
        modelBuilder.Entity<ArticleContent>().HasData(
            new ArticleContent
            {
                Id = 1000,
                ArticleId = 1000,
                Content = "# Velkommen\n\nUdforsk vores seneste artikler, vejledninger og indsigter. Hold dig opdateret med det bedste indhold, kurateret specielt til dig.",
                Excerpt = "Velkommen til vores hjemmeside",
                MetaTitle = "Forside",
                MetaDescription = "Udforsk vores seneste artikler og indsigter.",
                VersionNumber = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ArticleContent
            {
                Id = 1001,
                ArticleId = 1001,
                Content = "# Om os\n\nVelkommen til vores platform. Vores mission er at levere indhold af h\u00f8j kvalitet, der informerer, uddanner og inspirerer.\n\n## Vores historie\n\nVi startede med et simpelt m\u00e5l: at dele viden og indsigter med vores f\u00e6llesskab. Over tid er vi vokset til en platform, der rummer diverst indhold p\u00e5 tv\u00e6rs af forskellige emner.\n\n## Kontakt\n\nVi h\u00f8rer gerne fra dig! Uanset om du har feedback, forslag eller bare vil sige hej, er du velkommen til at kontakte os.",
                Excerpt = "L\u00e6r mere om os og vores mission",
                MetaTitle = "Om os",
                MetaDescription = "L\u00e6r mere om vores platform og holdet bag.",
                VersionNumber = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed default theme setting
        modelBuilder.Entity<SiteSettings>().HasData(new SiteSettings
        {
            Id = 1,
            Key = "theme",
            Value = "ocean-blue"
        });

        // Seed dark mode settings (disabled by default)
        // IDs 2-11 are used at runtime; use high IDs to avoid conflicts
        modelBuilder.Entity<SiteSettings>().HasData(
            new SiteSettings { Id = 100, Key = "dark_mode_admin", Value = "false" },
            new SiteSettings { Id = 101, Key = "dark_mode_web", Value = "false" }
        );

        // Seed navigations
        modelBuilder.Entity<Navigation>().HasData(
            new Navigation { Id = 1, Name = "Hovedmenu", Slug = "main-menu" },
            new Navigation { Id = 2, Name = "Footer-links", Slug = "footer-links" }
        );

        // Seed main menu items
        modelBuilder.Entity<NavigationItem>().HasData(
            new NavigationItem
            {
                Id = 1, NavigationId = 1, Label = "Hjem",
                ItemType = NavigationItemType.SystemRoute, SystemRoute = "/",
                Position = 1, OpenInNewTab = false
            },
            new NavigationItem
            {
                Id = 2, NavigationId = 1, Label = "Blog",
                ItemType = NavigationItemType.SystemRoute, SystemRoute = "/blog",
                Position = 2, OpenInNewTab = false
            },
            new NavigationItem
            {
                Id = 3, NavigationId = 1, Label = "Kategorier",
                ItemType = NavigationItemType.SystemRoute, SystemRoute = "/categories",
                Position = 3, OpenInNewTab = false
            },
            new NavigationItem
            {
                Id = 4, NavigationId = 1, Label = "Serier",
                ItemType = NavigationItemType.SystemRoute, SystemRoute = "/series",
                Position = 4, OpenInNewTab = false
            },
            new NavigationItem
            {
                Id = 5, NavigationId = 1, Label = "Om",
                ItemType = NavigationItemType.Page, ArticleId = 1001,
                Position = 5, OpenInNewTab = false
            },
            // Seed footer items
            new NavigationItem
            {
                Id = 6, NavigationId = 2, Label = "Hjem",
                ItemType = NavigationItemType.SystemRoute, SystemRoute = "/",
                Position = 1, OpenInNewTab = false
            },
            new NavigationItem
            {
                Id = 7, NavigationId = 2, Label = "Blog",
                ItemType = NavigationItemType.SystemRoute, SystemRoute = "/blog",
                Position = 2, OpenInNewTab = false
            },
            new NavigationItem
            {
                Id = 8, NavigationId = 2, Label = "Om",
                ItemType = NavigationItemType.Page, ArticleId = 1001,
                Position = 3, OpenInNewTab = false
            }
        );
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                    result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }
}
