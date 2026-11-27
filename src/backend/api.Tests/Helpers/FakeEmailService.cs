using System.Collections.Concurrent;
using SharpQuill.Application.Email;

namespace SharpQuill.Api.Tests.Helpers;

/// <summary>Records messages instead of sending them over SMTP.</summary>
public class FakeEmailService : IEmailService
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public bool IsConfigured { get; set; } = true;

    public IReadOnlyList<EmailMessage> Sent => _sent.ToArray();

    public Task SendAsync(EmailMessage message)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public void Reset()
    {
        _sent.Clear();
        IsConfigured = true;
    }
}
