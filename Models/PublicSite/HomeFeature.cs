namespace Models.PublicSite;

public class HomeFeature
{
    public string? ImageUrl { get; set; }

    public required string Name { get; set; }

    public string? Text { get; set; }

    public int SortOrder { get; set; }
}
