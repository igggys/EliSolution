namespace Models.PublicSite;

/// <summary>
/// Complete data needed to render the Terms of Use page on the public site.
/// </summary>
public class TermsOfUsePage
{
    /// <summary>Resolved language used for this page (content, menu, html lang/dir).</summary>
    public required Language CurrentLanguage { get; set; }

    /// <summary>Active languages for the language switcher.</summary>
    public IReadOnlyList<Language> Languages { get; set; } = [];

    /// <summary>Two-level site menu for <see cref="CurrentLanguage"/>.</summary>
    public required Menu Menu { get; set; }

    /// <summary>Terms of Use page content; null when the page was not found.</summary>
    public TermsOfUse? TermsOfUse { get; set; }

    /// <summary>Site-wide contact information (email, phone, WhatsApp).</summary>
    public ContactInfo? ContactInfo { get; set; }
}
