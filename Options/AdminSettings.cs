namespace Options;

public class AdminSettings
{
    public const string SectionName = "Admin";

    public string? BootstrapEmail { get; set; }

    public string? BootstrapPassword { get; set; }

    public string? BootstrapDisplayName { get; set; }
}
