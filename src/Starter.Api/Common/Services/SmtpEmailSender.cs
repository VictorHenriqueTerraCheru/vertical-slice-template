using MailKit.Net.Smtp;
using MailKit.Security;

using MimeKit;
using MimeKit.Text;

namespace Starter.Api.Common;

public sealed class SmtpEmailSender(EnvironmentConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(configuration.SmtpSenderName, configuration.SmtpSenderEmail!));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new TextPart(TextFormat.Plain) { Text = message.Body };

        var security = configuration.SmtpPort == SmtpConstants.ImplicitTlsPort
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        using var client = new SmtpClient { Timeout = SmtpConstants.TimeoutMilliseconds };
        await client.ConnectAsync(configuration.SmtpHost!, configuration.SmtpPort, security, cancellationToken);
        await client.AuthenticateAsync(configuration.SmtpUser!, configuration.SmtpPassword!, cancellationToken);
        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
