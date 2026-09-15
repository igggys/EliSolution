namespace Models.PublicSite;

public class PracticeAreaLink
{
    public string? Url { get; set; }

    public required string Name { get; set; }

    public string? Summary { get; set; }

    public string? ImageUrl { get; set; }

    public string? IconUrl { get; set; }

    public int SortOrder { get; set; }
}
