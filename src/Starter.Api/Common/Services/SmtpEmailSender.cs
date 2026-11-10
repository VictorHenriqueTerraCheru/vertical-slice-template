using MailKit.Net.Smtp;
using MailKit.Security;

using MimeKit;

namespace Starter.Api.Common;

public sealed class SmtpEmailSender(EnvironmentConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(configuration.SmtpSenderName, configuration.SmtpSenderEmail!));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody();

        using var client = new SmtpClient { Timeout = SmtpConstants.TimeoutMilliseconds };
        await client.ConnectAsync(configuration.SmtpHost!, configuration.SmtpPort, SocketOptions(configuration.SmtpSecurity), cancellationToken);
        if (!string.IsNullOrWhiteSpace(configuration.SmtpUser))
        {
            await client.AuthenticateAsync(configuration.SmtpUser, configuration.SmtpPassword ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static SecureSocketOptions SocketOptions(SmtpSecurity security) => security switch
    {
        SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
        SmtpSecurity.None => SecureSocketOptions.None,
        _ => SecureSocketOptions.StartTls,
    };
}
