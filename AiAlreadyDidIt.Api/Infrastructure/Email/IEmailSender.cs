using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AiAlreadyDidIt.Api.Infrastructure.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default);
}

/// <summary>Development sender: writes the message to the log.</summary>
public sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        logger.LogInformation("EMAIL to {To} | {Subject}\n{Body}", to, subject, textBody ?? htmlBody);
        return Task.CompletedTask;
    }
}

public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _o = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_o.FromName, _o.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        var builder = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody };
        message.Body = builder.ToMessageBody();
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_o.SmtpHost, _o.SmtpPort, _o.SmtpUseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable, ct);
            if (!string.IsNullOrEmpty(_o.SmtpUser)) await client.AuthenticateAsync(_o.SmtpUser, _o.SmtpPassword, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP send to {To} failed", to);
            throw;
        }
    }
}
