namespace Models.PublicSite;

public class PracticeArea
{
    public string? Url { get; set; }

    public required string Name { get; set; }

    public string? Body { get; set; }

    public string? ImageUrl { get; set; }

    public string? IconUrl { get; set; }

    public string? SeoTitle { get; set; }

    public string? MetaDescription { get; set; }

    public required string MetaRobots { get; set; }

    public string? OgTitle { get; set; }

    public string? OgDescription { get; set; }

    public string? OgImage { get; set; }

    public required string OgType { get; set; }

    public string? OgUrl { get; set; }
}
