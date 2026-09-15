namespace Services.MailService;

using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class MailService : IMailService
{
    private const int MaxFieldLength = 200;
    private const int MaxMessageLength = 8000;

    private readonly MailSettings _settings;

    public MailService(MailSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    public async Task SendContactMessageAsync(
        ContactMessage message,
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.FullName))
        {
            throw new MailServiceException("Contact message is missing a name.");
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new MailServiceException("Recipient email is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Host)
            || string.IsNullOrWhiteSpace(_settings.UserName)
            || string.IsNullOrWhiteSpace(_settings.Password))
        {
            throw new MailServiceException("Mail settings are incomplete.");
        }

        var fullName = Sanitize(message.FullName, MaxFieldLength);
        var phone = Sanitize(message.Phone, MaxFieldLength);
        var senderEmail = Sanitize(message.Email, MaxFieldLength);
        var bodyText = TrimToLength(message.Message, MaxMessageLength);

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(
            string.IsNullOrWhiteSpace(_settings.FromName) ? _settings.UserName : _settings.FromName,
            _settings.UserName));
        mime.To.Add(MailboxAddress.Parse(recipientEmail.Trim()));
        mime.Subject = $"פנייה חדשה מהאתר: {fullName}";
        mime.Body = new TextPart("plain")
        {
            Text = BuildBody(fullName, phone, senderEmail, bodyText)
        };

        if (MailboxAddress.TryParse(senderEmail, out var replyTo))
        {
            mime.ReplyTo.Add(new MailboxAddress(fullName, replyTo.Address));
        }

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                SocketOptions(),
                cancellationToken);
            await client.AuthenticateAsync(
                _settings.UserName,
                _settings.Password.Replace(" ", string.Empty, StringComparison.Ordinal),
                cancellationToken);
            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (MailServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new MailServiceException("Failed to send contact message.", ex);
        }
    }

    private SecureSocketOptions SocketOptions()
    {
        if (_settings.Port == 465)
        {
            return SecureSocketOptions.SslOnConnect;
        }

        return _settings.EnableSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;
    }

    private static string BuildBody(string fullName, string phone, string email, string message)
    {
        var builder = new StringBuilder();
        builder.AppendLine("פנייה חדשה מאתר האינטרנט");
        builder.AppendLine();
        builder.AppendLine($"שם מלא: {fullName}");
        builder.AppendLine($"טלפון: {phone}");
        builder.AppendLine($"דוא\"ל: {email}");
        builder.AppendLine();
        builder.AppendLine("הודעה:");
        builder.AppendLine(message);
        return builder.ToString();
    }

    private static string Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = value
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        return TrimToLength(cleaned, maxLength);
    }

    private static string TrimToLength(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
