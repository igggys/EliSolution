namespace Models.PublicSite;

/// <summary>
/// Complete data needed to render the Home page on the public site.
/// </summary>
public class HomePage
{
    /// <summary>Resolved language used for this page (content, menu, html lang/dir).</summary>
    public required Language CurrentLanguage { get; set; }

    /// <summary>Active languages for the language switcher.</summary>
    public IReadOnlyList<Language> Languages { get; set; } = [];

    /// <summary>Two-level site menu for <see cref="CurrentLanguage"/>.</summary>
    public required Menu Menu { get; set; }

    /// <summary>Practice area sections for the home grid.</summary>
    public IReadOnlyList<PracticeAreaLink> PracticeAreas { get; set; } = [];

    /// <summary>Featured blog cards (or latest posts if none are featured).</summary>
    public IReadOnlyList<BlogPostLink> FeaturedBlogs { get; set; } = [];

    /// <summary>Site-wide contact information (email, phone, WhatsApp).</summary>
    public ContactInfo? ContactInfo { get; set; }

    public string? HeroEyebrow { get; set; }

    public string? HeroTitle { get; set; }

    public string? PageText { get; set; }

    public string? HeroCtaText { get; set; }

    public string? HeroCtaUrl { get; set; }

    public string? HeroVideoUrl { get; set; }

    public IReadOnlyList<HomeFeature> Features { get; set; } = [];

    public string? StripBannerText { get; set; }

    public string? SeoTitle { get; set; }

    public string? MetaDescription { get; set; }

    public required string MetaRobots { get; set; }

    public string? OgTitle { get; set; }

    public string? OgDescription { get; set; }

    public string? OgImage { get; set; }

    public required string OgType { get; set; }

    public string? OgUrl { get; set; }
}
