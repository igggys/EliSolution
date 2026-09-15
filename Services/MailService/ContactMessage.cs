namespace Services.MailService;

public class ContactMessage
{
    public required string FullName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Message { get; set; }
}
