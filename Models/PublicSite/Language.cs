namespace Models.PublicSite;

public class Language
{
    public required string Code { get; set; }

    public required string NativeName { get; set; }

    public bool IsRtl { get; set; }

    public bool IsDefault { get; set; }

    public int SortOrder { get; set; }
}
