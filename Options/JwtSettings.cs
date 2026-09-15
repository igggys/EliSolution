namespace Options;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; set; }

    public required string Audience { get; set; }

    public required string SigningKey { get; set; }

    public int AccessTokenHours { get; set; } = 8;
}
