namespace Models.PublicSite;

/// <summary>
/// Chrome data needed to render the 404 page on the public site.
/// </summary>
public class NotFoundPage
{
    /// <summary>Resolved language used for this page (menu, html lang/dir).</summary>
    public required Language CurrentLanguage { get; set; }

    /// <summary>Active languages for the language switcher.</summary>
    public IReadOnlyList<Language> Languages { get; set; } = [];

    /// <summary>Two-level site menu for <see cref="CurrentLanguage"/>.</summary>
    public required Menu Menu { get; set; }

    /// <summary>Site-wide contact information (email, phone, WhatsApp).</summary>
    public ContactInfo? ContactInfo { get; set; }
}
