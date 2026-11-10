namespace Starter.Api.Common;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email to {To} with subject {Subject}: {Body}", message.To, message.Subject, message.TextBody);
        return Task.CompletedTask;
    }
}
