namespace Models.PublicSite;

/// <summary>
/// Complete data needed to render a Practice Area page on the public site.
/// </summary>
public class PracticeAreaPage
{
    /// <summary>Resolved language used for this page (content, menu, html lang/dir).</summary>
    public required Language CurrentLanguage { get; set; }

    /// <summary>Active languages for the language switcher.</summary>
    public IReadOnlyList<Language> Languages { get; set; } = [];

    /// <summary>Two-level site menu for <see cref="CurrentLanguage"/>.</summary>
    public required Menu Menu { get; set; }

    /// <summary>All practice area sections for navigation links on the page.</summary>
    public IReadOnlyList<PracticeAreaLink> PracticeAreas { get; set; } = [];

    /// <summary>Selected page content; null when the section was not found.</summary>
    public PracticeArea? PracticeArea { get; set; }

    /// <summary>Site-wide contact information (email, phone, WhatsApp).</summary>
    public ContactInfo? ContactInfo { get; set; }
}
