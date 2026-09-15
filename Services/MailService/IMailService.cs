namespace Services.MailService;

public interface IMailService
{
    Task SendContactMessageAsync(
        ContactMessage message,
        string recipientEmail,
        CancellationToken cancellationToken = default);
}
