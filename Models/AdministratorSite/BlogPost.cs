namespace Models.AdministratorSite;

public class BlogPost
{
    public int Id { get; set; }

    public byte LanguageId { get; set; }

    public required string Url { get; set; }

    public required string Name { get; set; }

    public string? Body { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? PublishedAt { get; set; }

    public bool IsPublished { get; set; } = true;

    public int? SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public string? SeoTitle { get; set; }

    public string? MetaDescription { get; set; }

    public string? MetaRobots { get; set; }

    public string? OgTitle { get; set; }

    public string? OgDescription { get; set; }

    public string? OgImage { get; set; }

    public string? OgType { get; set; }

    public string? OgUrl { get; set; }

    public bool IsFeatured { get; set; }
}
