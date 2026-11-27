namespace SharpQuill.Application.Email;

public interface IEmailService
{
    /// <summary>False until the SMTP settings needed to send are present.</summary>
    bool IsConfigured { get; }

    Task SendAsync(EmailMessage message);
}

public record EmailMessage(
    string ToAddress,
    string ToName,
    string Subject,
    string HtmlBody,
    string? ReplyToAddress = null,
    string? ReplyToName = null
);
