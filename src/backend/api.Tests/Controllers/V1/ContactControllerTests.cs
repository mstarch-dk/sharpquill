using System.Net;
using System.Net.Http.Json;
using SharpQuill.Api.Tests.Fixtures;
using SharpQuill.Api.Tests.Helpers;
using SharpQuill.Application.Email;
using FluentAssertions;
using Xunit;

namespace SharpQuill.Api.Tests.Controllers.V1;

/// <summary>
/// The contact form mails the site's contact address through IEmailService (a fake here). It answers 503 until both
/// the contact_email setting and SMTP are configured. The seed sets contact_email to test@example.com.
/// </summary>
[Collection("Database")]
public class ContactControllerTests
{
    private const string Url = "/api/v1/contact";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;
    private readonly FakeEmailService _email;

    public ContactControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient().WithUniqueClientIp();
        _email = fixture.Factory.FakeEmailService;
        _email.Reset();
    }

    private static object ValidMessage(string? subject = "Hello") => new
    {
        Name = "Jane Reader",
        Email = "jane@example.com",
        Subject = subject,
        Message = "Line one\nLine two"
    };

    [Fact]
    public async Task Send_WhenConfigured_MailsContactAddressWithReplyToSender()
    {
        var response = await _client.PostAsJsonAsync(Url, ValidMessage());

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var message = _email.Sent.Should().ContainSingle().Subject;
        message.ToAddress.Should().Be("test@example.com");
        message.ToName.Should().Be("Test CMS");
        message.Subject.Should().Be("Hello");
        message.ReplyToAddress.Should().Be("jane@example.com");
        message.ReplyToName.Should().Be("Jane Reader");
        message.HtmlBody.Should().Contain("Jane Reader &lt;jane@example.com&gt;");
        message.HtmlBody.Should().Contain("Line one<br/>Line two");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Send_WithoutSubject_UsesFallbackSubject(string? subject)
    {
        var response = await _client.PostAsJsonAsync(Url, ValidMessage(subject));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _email.Sent.Should().ContainSingle().Which.Subject.Should().Be("Contact form message from Jane Reader");
    }

    [Fact]
    public async Task Send_HtmlEncodesUserInput()
    {
        var response = await _client.PostAsJsonAsync(Url, new
        {
            Name = "<b>Mallory</b>",
            Email = "mallory@example.com",
            Subject = "<script>alert(1)</script>",
            Message = "<img src=x onerror=alert(1)>"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var body = _email.Sent.Should().ContainSingle().Subject.HtmlBody;
        body.Should().NotContain("<b>Mallory</b>").And.NotContain("<script>").And.NotContain("<img");
        body.Should().Contain("&lt;b&gt;Mallory&lt;/b&gt;");
        body.Should().Contain("&lt;img src=x onerror=alert(1)&gt;");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Send_WithoutContactEmailSetting_ReturnsServiceUnavailable(string contactEmail)
    {
        var response = await TestSql.WithSiteSettingAsync("contact_email", contactEmail,
            () => _client.PostAsJsonAsync(Url, ValidMessage()));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        _email.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_WithoutSmtpConfigured_ReturnsServiceUnavailable()
    {
        _email.IsConfigured = false;

        var response = await _client.PostAsJsonAsync(Url, ValidMessage());

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        _email.Sent.Should().BeEmpty();
    }

    public static TheoryData<string, string, string?, string> InvalidMessages => new()
    {
        { "", "jane@example.com", null, "Hi" },
        { new string('n', 101), "jane@example.com", null, "Hi" },
        { "Jane", "", null, "Hi" },
        { "Jane", "not-an-email", null, "Hi" },
        { "Jane", "jane@example.com", new string('s', 201), "Hi" },
        { "Jane", "jane@example.com", null, "" },
        { "Jane", "jane@example.com", null, new string('m', 5001) },
    };

    [Theory]
    [MemberData(nameof(InvalidMessages))]
    public async Task Send_WithInvalidInput_ReturnsBadRequest(string name, string email, string? subject, string message)
    {
        var response = await _client.PostAsJsonAsync(Url, new { Name = name, Email = email, Subject = subject, Message = message });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _email.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_MoreThanThreePerMinuteFromOneIp_IsRateLimited()
    {
        for (var i = 0; i < 3; i++)
        {
            (await _client.PostAsJsonAsync(Url, ValidMessage())).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var limited = await _client.PostAsJsonAsync(Url, ValidMessage());

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        _email.Sent.Should().HaveCount(3);
    }
}

public class EmailSettingsTests
{
    private static EmailSettings Complete() => new()
    {
        Host = "smtp.example.com",
        Username = "user",
        Password = "secret",
        FromAddress = "noreply@example.com"
    };

    [Fact]
    public void IsConfigured_WithHostCredentialsAndSender_IsTrue()
    {
        Complete().IsConfigured.Should().BeTrue();
    }

    [Theory]
    [InlineData("Host")]
    [InlineData("Username")]
    [InlineData("Password")]
    [InlineData("FromAddress")]
    public void IsConfigured_WithARequiredValueMissing_IsFalse(string missing)
    {
        var settings = Complete();
        typeof(EmailSettings).GetProperty(missing)!.SetValue(settings, " ");

        settings.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithDefaults_IsFalse()
    {
        // The default host alone (as with the dev stack and the deploy .env.example) is not enough to send
        new EmailSettings().IsConfigured.Should().BeFalse();
    }
}
