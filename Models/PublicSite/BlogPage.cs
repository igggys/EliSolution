namespace Models.PublicSite;

/// <summary>
/// Complete data needed to render the blog listing page on the public site.
/// </summary>
public class BlogPage
{
    /// <summary>Resolved language used for this page (content, menu, html lang/dir).</summary>
    public required Language CurrentLanguage { get; set; }

    /// <summary>Active languages for the language switcher.</summary>
    public IReadOnlyList<Language> Languages { get; set; } = [];

    /// <summary>Two-level site menu for <see cref="CurrentLanguage"/>.</summary>
    public required Menu Menu { get; set; }

    /// <summary>Published blog posts for the current listing page.</summary>
    public IReadOnlyList<BlogPostLink> Posts { get; set; } = [];

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 6;

    public int TotalCount { get; set; }

    public int TotalPages =>
        PageSize <= 0 || TotalCount <= 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => TotalPages > 0 && PageNumber < TotalPages;

    /// <summary>Site-wide contact information (email, phone, WhatsApp).</summary>
    public ContactInfo? ContactInfo { get; set; }
}
