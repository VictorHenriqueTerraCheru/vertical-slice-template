using System.Collections.Concurrent;

using Starter.Api.Common;

namespace Starter.Api.Tests.Infra;

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public bool FailOnSend { get; set; }

    public IReadOnlyList<EmailMessage> SentTo(string email) =>
        _messages.Where(message => message.To == email).ToList();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (FailOnSend)
        {
            throw new InvalidOperationException("Simulated delivery failure.");
        }

        _messages.Enqueue(message);
        return Task.CompletedTask;
    }
}
