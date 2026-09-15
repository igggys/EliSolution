namespace Models.PublicSite;

/// <summary>
/// Complete data needed to render the Privacy Policy page on the public site.
/// </summary>
public class PrivacyPolicyPage
{
    /// <summary>Resolved language used for this page (content, menu, html lang/dir).</summary>
    public required Language CurrentLanguage { get; set; }

    /// <summary>Active languages for the language switcher.</summary>
    public IReadOnlyList<Language> Languages { get; set; } = [];

    /// <summary>Two-level site menu for <see cref="CurrentLanguage"/>.</summary>
    public required Menu Menu { get; set; }

    /// <summary>Privacy Policy page content; null when the page was not found.</summary>
    public PrivacyPolicy? PrivacyPolicy { get; set; }

    /// <summary>Site-wide contact information (email, phone, WhatsApp).</summary>
    public ContactInfo? ContactInfo { get; set; }
}
