using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SharpQuill.Application.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public SmtpEmailService(EmailSettings settings)
    {
        _settings = settings;
    }

    public bool IsConfigured => _settings.IsConfigured;

    public async Task SendAsync(EmailMessage message)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        email.To.Add(new MailboxAddress(message.ToName, message.ToAddress));

        if (message.ReplyToAddress is not null)
            email.ReplyTo.Add(new MailboxAddress(message.ReplyToName ?? message.ReplyToAddress, message.ReplyToAddress));

        email.Subject = message.Subject;
        email.Body = new TextPart("html") { Text = message.HtmlBody };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
        await smtp.SendAsync(new SendRequest(email));
        await smtp.DisconnectAsync(true);
    }
}
