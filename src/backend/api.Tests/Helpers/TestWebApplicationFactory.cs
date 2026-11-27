using SharpQuill.Application.Email;
using SharpQuill.Application.LinkPreviews.Services;
using SharpQuill.Application.TweetPreviews.Services;
using SharpQuill.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;

namespace SharpQuill.Api.Tests.Helpers;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<ILinkPreviewService> MockLinkPreviewService { get; } = new();
    public Mock<ITweetPreviewService> MockTweetPreviewService { get; } = new();
    public FakeEmailService FakeEmailService { get; } = new();

    // Uploads go to a throwaway directory instead of the API project's uploads folder.
    public string UploadsPath { get; } = Path.Combine(Path.GetTempPath(), "cms-tests", Guid.NewGuid().ToString("N"), "uploads");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // All test requests share one client IP; keep auth rate limits out of the way of other tests.
        builder.UseSetting("RateLimiting:AuthPermitLimit", "10000");
        builder.UseSetting("RateLimiting:RefreshPermitLimit", "10000");
        builder.UseSetting("Uploads:Path", UploadsPath);

        builder.ConfigureServices(services =>
        {
            // Remove the existing NpgsqlDataSource registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(NpgsqlDataSource));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Every host (including WithWebHostBuilder derivatives) shares one bounded pool; see TestDatabase.
            services.AddSingleton(TestDatabase.DataSource);

            // Replace LinkPreviewService with mock to avoid real HTTP requests
            var lpDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ILinkPreviewService));
            if (lpDescriptor != null)
            {
                services.Remove(lpDescriptor);
            }
            services.AddSingleton<ILinkPreviewService>(MockLinkPreviewService.Object);

            // Replace TweetPreviewService with mock to avoid real HTTP requests
            var tpDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITweetPreviewService));
            if (tpDescriptor != null)
            {
                services.Remove(tpDescriptor);
            }
            services.AddSingleton<ITweetPreviewService>(MockTweetPreviewService.Object);

            // Replace the SMTP email service so the contact form never sends real mail
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null)
            {
                services.Remove(emailDescriptor);
            }
            services.AddSingleton<IEmailService>(FakeEmailService);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        var testRoot = Path.GetDirectoryName(UploadsPath)!;
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }
}
