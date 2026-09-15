namespace Models.PublicSite;

public class MenuItem
{
    public string? Url { get; set; }

    public required string Title { get; set; }

    public IReadOnlyList<MenuItem> SubMenu { get; set; } = [];
}
