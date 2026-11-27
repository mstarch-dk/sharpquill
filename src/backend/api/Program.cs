using SharpQuill.Api.Infrastructure;
using SharpQuill.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Cors.Infrastructure;
using SharpQuill.Api.Infrastructure.Filters;
using SharpQuill.Api.Infrastructure.Middleware;
using SharpQuill.Application.Articles.Repositories;
using SharpQuill.Application.Articles.Services;
using SharpQuill.Application.Articles.Suggestions;
using SharpQuill.Application.Auth;
using SharpQuill.Application.Auth.Repositories;
using SharpQuill.Application.Auth.Services;
using SharpQuill.Application.Auth.Validators;
using SharpQuill.Application.Authors.Repositories;
using SharpQuill.Application.Authors.Services;
using SharpQuill.Application.Categories.Repositories;
using SharpQuill.Application.Categories.Services;
using SharpQuill.Application.ContentPackage.Services;
using SharpQuill.Application.LinkPreviews.Services;
using SharpQuill.Application.TweetPreviews.Services;
using SharpQuill.Application.Media.Repositories;
using SharpQuill.Application.Media.Services;
using SharpQuill.Application.Navigations.Repositories;
using SharpQuill.Application.Navigations.Services;
using SharpQuill.Application.Comments.Repositories;
using SharpQuill.Application.Comments.Services;
using SharpQuill.Application.Likes.Repositories;
using SharpQuill.Application.Likes.Services;
using SharpQuill.Application.Series.Repositories;
using SharpQuill.Application.Series.Services;
using SharpQuill.Application.WebsiteUsers.Repositories;
using SharpQuill.Application.WebsiteUsers.Services;
using SharpQuill.Application.Settings.Repositories;
using SharpQuill.Application.Settings.Services;
using SharpQuill.Application.Tags.Repositories;
using SharpQuill.Application.Tags.Services;
using SharpQuill.Application.Email;
using SharpQuill.Core.Articles;
using SharpQuill.Core.Articles.Suggestions;
using SharpQuill.Core.Auth;
using SharpQuill.Core.Authors;
using SharpQuill.Core.Categories;
using SharpQuill.Core.Comments;
using SharpQuill.Core.Likes;
using SharpQuill.Core.Media;
using SharpQuill.Core.Navigations;
using SharpQuill.Core.Series;
using SharpQuill.Core.Settings;
using SharpQuill.Core.Tags;
using SharpQuill.Core.WebsiteUsers;
using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;
using Npgsql;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add controllers with validation filter and camelCase JSON serialization
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// Add Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sharpquill API",
        Version = "v1",
        Description = "Public API for Sharpquill"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

// CORS per audience: admin endpoints trust the admin origin(s), everything else the public website origin(s).
// Cors:Origins is the legacy single list and is used for both audiences when the specific lists are absent.
var legacyCorsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
var adminCorsOrigins = builder.Configuration.GetSection("Cors:AdminOrigins").Get<string[]>() ?? legacyCorsOrigins;
var publicCorsOrigins = builder.Configuration.GetSection("Cors:PublicOrigins").Get<string[]>() ?? legacyCorsOrigins;
builder.Services.AddCors();
builder.Services.AddSingleton<ICorsPolicyProvider>(new AudienceCorsPolicyProvider(adminCorsOrigins, publicCorsOrigins));

// Configure JWT settings
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing")
    && jwtSettings.ValidateSecret() is { } jwtSecretProblem)
{
    // Tokens signed with a known or weak key can be forged by anyone, so refuse to start
    throw new InvalidOperationException(
        $"Invalid Jwt:Secret: {jwtSecretProblem}. Set Jwt__Secret to a random value of at least {JwtSettings.MinimumSecretLength} characters.");
}
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddCmsAuthentication(jwtSettings);

// Configure Email settings
var emailSettings = new EmailSettings();
builder.Configuration.GetSection(EmailSettings.SectionName).Bind(emailSettings);
builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Register NpgsqlDataSource as singleton
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=cms;Username=cms_user;Password=cms_password";
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

// Register repositories (scoped)
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IAdminArticleRepository, AdminArticleRepository>();
builder.Services.AddScoped<IArticleContentRepository, ArticleContentRepository>();
builder.Services.AddScoped<IAdminCategoryRepository, AdminCategoryRepository>();
builder.Services.AddScoped<IAdminTagRepository, AdminTagRepository>();
builder.Services.AddScoped<ISeriesRepository, SeriesRepository>();
builder.Services.AddScoped<IAdminSeriesRepository, AdminSeriesRepository>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<ISiteSettingsRepository, SiteSettingsRepository>();
builder.Services.AddScoped<INavigationRepository, NavigationRepository>();
builder.Services.AddScoped<IAdminNavigationRepository, AdminNavigationRepository>();
builder.Services.AddScoped<IAdminAuthorRepository, AdminAuthorRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IAdminCommentRepository, AdminCommentRepository>();
builder.Services.AddScoped<IArticleLikeRepository, ArticleLikeRepository>();
builder.Services.AddScoped<IWebsiteUserRepository, WebsiteUserRepository>();
builder.Services.AddScoped<IAdminWebsiteUserRepository, AdminWebsiteUserRepository>();
builder.Services.AddScoped<IWebsiteLoginTokenRepository, WebsiteLoginTokenRepository>();
builder.Services.AddScoped<IWebsiteRefreshTokenRepository, WebsiteRefreshTokenRepository>();
builder.Services.AddScoped<ITitleAutocompleteRepository, TitleAutocompleteRepository>();

// Search-as-you-type suggestions: an in-memory index, loaded at startup and kept current by Postgres NOTIFY
builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SuggestionIndexHolder>();
builder.Services.AddSingleton<ISuggestionIndexRepository, SuggestionIndexRepository>();
builder.Services.AddSingleton<ISearchSuggestionService, SearchSuggestionService>();
builder.Services.AddHostedService<SuggestionIndexListener>();

// Configure uploads path
var uploadsPath = builder.Configuration["Uploads:Path"] ?? Path.Combine(builder.Environment.ContentRootPath, "uploads");
// Ensure path is absolute (required by PhysicalFileProvider)
if (!Path.IsPathRooted(uploadsPath))
{
    uploadsPath = Path.Combine(builder.Environment.ContentRootPath, uploadsPath);
}
uploadsPath = Path.GetFullPath(uploadsPath);
var uploadsUrlBase = builder.Configuration["Uploads:UrlBase"] ?? "/uploads";

// Ensure uploads directory exists
Directory.CreateDirectory(uploadsPath);

// Register layout composer
builder.Services.AddScoped<ILayoutComposer>(sp => new LayoutComposer(
    sp.GetRequiredService<IMediaRepository>(),
    uploadsUrlBase
));

// Register services (scoped)
builder.Services.AddScoped<IArticleService>(sp => new ArticleService(
    sp.GetRequiredService<IArticleRepository>(),
    sp.GetRequiredService<ISeriesService>(),
    sp.GetRequiredService<ILayoutComposer>(),
    uploadsUrlBase
));
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminArticleService>(sp => new AdminArticleService(
    sp.GetRequiredService<IAdminArticleRepository>(),
    sp.GetRequiredService<IArticleContentRepository>(),
    sp.GetRequiredService<ILayoutComposer>(),
    uploadsUrlBase
));
builder.Services.AddScoped<IAdminCategoryService, AdminCategoryService>();
builder.Services.AddScoped<IAdminTagService, AdminTagService>();
builder.Services.AddScoped<ISeriesService, SeriesService>();
builder.Services.AddScoped<IAdminSeriesService, AdminSeriesService>();
builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddScoped<IAdminNavigationService, AdminNavigationService>();
builder.Services.AddScoped<IAdminAuthorService, AdminAuthorService>();
builder.Services.AddSingleton<IImageResizer, ImageResizer>();
builder.Services.AddScoped<IMediaService>(sp => new MediaService(
    sp.GetRequiredService<IMediaRepository>(),
    sp.GetRequiredService<IImageResizer>(),
    uploadsPath,
    uploadsUrlBase
));
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IAdminCommentService, AdminCommentService>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<IWebsiteAuthService, WebsiteAuthService>();
builder.Services.AddScoped<IAdminWebsiteUserService, AdminWebsiteUserService>();
builder.Services.AddScoped<IContentPackageService>(sp => new ContentPackageService(
    sp.GetRequiredService<IAdminArticleRepository>(),
    sp.GetRequiredService<IArticleContentRepository>(),
    sp.GetRequiredService<ICategoryRepository>(),
    sp.GetRequiredService<IAdminCategoryRepository>(),
    sp.GetRequiredService<ITagRepository>(),
    sp.GetRequiredService<IAdminTagRepository>(),
    sp.GetRequiredService<ISeriesRepository>(),
    sp.GetRequiredService<IAdminSeriesRepository>(),
    sp.GetRequiredService<IMediaRepository>(),
    sp.GetRequiredService<ISiteSettingsRepository>(),
    uploadsPath,
    sp.GetRequiredService<IConfiguration>().GetValue(
        "ContentPackage:MaxDecompressedBytes", ContentPackageService.DefaultMaxDecompressedBytes)
));
builder.Services.AddHttpClient<ILinkPreviewService, LinkPreviewService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
})
// Link previews fetch author-supplied URLs; only allow connections to public addresses
.ConfigurePrimaryHttpMessageHandler(() => PublicAddressHttpHandler.Create());
builder.Services.AddHttpClient<ITweetPreviewService, TweetPreviewService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("comments", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("likes", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("contact", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1)
            }));
    // Credential endpoints (login, cert-login, signup) get a tight budget; refresh is called
    // routinely by the frontends, so it gets a looser one. Limits are overridable via config.
    options.AddPolicy("auth", context => AuthRateLimitPartition(context, "RateLimiting:AuthPermitLimit", 10));
    options.AddPolicy("auth-refresh", context => AuthRateLimitPartition(context, "RateLimiting:RefreshPermitLimit", 60));
});

// Behind Caddy, take the client IP from X-Forwarded-For so per-IP rate limits apply per client
// instead of to the proxy. In production the API port is bound to 127.0.0.1 and only reachable
// through Caddy, which replaces any client-supplied X-Forwarded-For; ForwardLimit = 1 uses the
// entry Caddy added.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Backfill missing media variants once per environment to make featured image presets effective for existing uploads.
var mediaVariantBackfillMarkerPath = Path.Combine(uploadsPath, ".media-variants-backfill-v1.done");
if (!app.Environment.IsEnvironment("Testing") && !File.Exists(mediaVariantBackfillMarkerPath))
{
    try
    {
        await using var scope = app.Services.CreateAsyncScope();
        var mediaService = scope.ServiceProvider.GetRequiredService<IMediaService>();
        var processed = await mediaService.RegenerateVariantsAsync();

        await File.WriteAllTextAsync(
            mediaVariantBackfillMarkerPath,
            $"{DateTime.UtcNow:O}|processed={processed}"
        );

        app.Logger.LogInformation("Media variant backfill completed. Processed {ProcessedCount} items.", processed);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Media variant backfill failed; continuing startup without marker.");
    }
}

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sharpquill API v1");
    });
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors();
app.UseRateLimiter();

// Serve static files for uploads
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = uploadsUrlBase,
    OnPrepareResponse = ctx =>
    {
        // Uploads are served from the site's own origins; never let browsers reinterpret them as HTML or script
        ctx.Context.Response.Headers.XContentTypeOptions = "nosniff";
        // Opening a file directly gives a sandboxed, script-less document with an opaque origin, so an upload can
        // never act on the site's origin (images embedded with <img> are unaffected). Also covers legacy SVG files.
        ctx.Context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; sandbox";
        // PDFs are downloaded rather than rendered by an in-browser viewer on the site's origin
        if (ctx.File.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.ContentDisposition = "attachment";
        }
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static RateLimitPartition<string> AuthRateLimitPartition(HttpContext context, string permitLimitKey, int defaultPermitLimit)
{
    var permitLimit = context.RequestServices.GetRequiredService<IConfiguration>()
        .GetValue(permitLimitKey, defaultPermitLimit);

    return RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1)
        });
}

// Make Program accessible for integration tests
public partial class Program { }
