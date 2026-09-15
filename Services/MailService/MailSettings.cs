namespace Services.MailService;

public class MailSettings
{
    public const string SectionName = "Mail";

    public required string Host { get; set; }

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public required string UserName { get; set; }

    public required string Password { get; set; }

    public string? FromName { get; set; }
}
