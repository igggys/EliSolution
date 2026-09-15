namespace Models.PublicSite;

public class BlogPostLink
{
    public string? Url { get; set; }

    public required string Name { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>Short teaser for listing cards (from <c>MetaDescription</c>).</summary>
    public string? Excerpt { get; set; }
}
