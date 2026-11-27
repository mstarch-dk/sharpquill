using SharpQuill.Api.Infrastructure.Models;
using SharpQuill.Application.Contact.DTOs;
using SharpQuill.Application.Email;
using SharpQuill.Application.Settings.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SharpQuill.Api.Controllers.V1;

[ApiController]
[Route("api/v1/contact")]
public class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ISiteSettingsService _siteSettingsService;

    public ContactController(IEmailService emailService, ISiteSettingsService siteSettingsService)
    {
        _emailService = emailService;
        _siteSettingsService = siteSettingsService;
    }

    [HttpPost]
    [EnableRateLimiting("contact")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Send([FromBody] ContactFormDto dto)
    {
        var settings = await _siteSettingsService.GetAllAsync();

        if (string.IsNullOrWhiteSpace(settings.ContactEmail) || !_emailService.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ApiErrorResponse("SERVICE_UNAVAILABLE", "Contact form is not configured."));

        var subject = string.IsNullOrWhiteSpace(dto.Subject)
            ? $"Contact form message from {dto.Name}"
            : dto.Subject;

        var body = $"""
            <p><strong>From:</strong> {HtmlEncode(dto.Name)} &lt;{HtmlEncode(dto.Email)}&gt;</p>
            <p><strong>Subject:</strong> {HtmlEncode(subject)}</p>
            <hr/>
            <p>{HtmlEncode(dto.Message).Replace("\n", "<br/>")}</p>
            """;

        var message = new EmailMessage(
            ToAddress: settings.ContactEmail,
            ToName: settings.SiteName ?? "Site Owner",
            Subject: subject,
            HtmlBody: body,
            ReplyToAddress: dto.Email,
            ReplyToName: dto.Name
        );

        await _emailService.SendAsync(message);
        return NoContent();
    }

    private static string HtmlEncode(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
