namespace Helpers;

public static class PracticeAreaRoutes
{
    private static readonly Dictionary<string, string> SectionToDbUrl = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HiTech"] = "/hi-tech",
        ["Commercial"] = "/commercial",
        ["PrivacyProtection"] = "/privacy-protection"
    };

    private static readonly Dictionary<string, string> DbUrlToSection = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/hi-tech"] = "HiTech",
        ["/commercial"] = "Commercial",
        ["/privacy-protection"] = "PrivacyProtection"
    };

    public static string? ToDbUrl(string? section)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            return null;
        }

        return SectionToDbUrl.TryGetValue(section.Trim(), out var dbUrl)
            ? dbUrl
            : null;
    }

    public static string? ToSection(string? dbUrl)
    {
        if (string.IsNullOrWhiteSpace(dbUrl))
        {
            return null;
        }

        var normalized = dbUrl.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return DbUrlToSection.TryGetValue(normalized, out var section)
            ? section
            : null;
    }

    public static bool IsPracticeAreaUrl(string? dbUrl)
        => ToSection(dbUrl) is not null;

    public static string BuildPath(string? dbUrl, string languageCode)
    {
        var section = ToSection(dbUrl);
        if (section is null || string.IsNullOrWhiteSpace(languageCode))
        {
            return dbUrl ?? "#";
        }

        return $"/PracticeArea/{section}/{languageCode}";
    }
}
