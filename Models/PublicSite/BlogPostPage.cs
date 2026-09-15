namespace Models.PublicSite;

/// <summary>
/// Complete data needed to render a single blog article on the public site.
/// </summary>
public class BlogPostPage
{
    /// <summary>Resolved language used for this page (content, menu, html lang/dir).</summary>
    public required Language CurrentLanguage { get; set; }

    /// <summary>Active languages for the language switcher.</summary>
    public IReadOnlyList<Language> Languages { get; set; } = [];

    /// <summary>Two-level site menu for <see cref="CurrentLanguage"/>.</summary>
    public required Menu Menu { get; set; }

    /// <summary>Selected article; null when the post was not found.</summary>
    public BlogPost? Post { get; set; }

    /// <summary>Other recent published posts for the sidebar.</summary>
    public IReadOnlyList<BlogPostLink> LatestPosts { get; set; } = [];

    /// <summary>Site-wide contact information (email, phone, WhatsApp).</summary>
    public ContactInfo? ContactInfo { get; set; }
}
