namespace Helpers;

public static class BlogRoutes
{
    private const string DbPrefix = "/blog/";

    /// <summary>
    /// Route segment for an article slug: letters in any script, marks, digits, spaces, hyphens.
    /// Length 3+ so two-letter language codes still hit the blog listing route.
    /// </summary>
    public const string SlugConstraint = @"[\p{L}\p{M}\p{N}\p{Zs}\-]{3,}";

    public static string? ToDbUrl(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var value = DecodeSlug(slug.Trim().Trim('/'));
        if (value.Length == 0 || value.Contains('/'))
        {
            return null;
        }

        return DbPrefix + value.ToLowerInvariant();
    }

    public static string? ToSlug(string? dbUrl)
    {
        if (!IsBlogPostUrl(dbUrl))
        {
            return null;
        }

        var normalized = dbUrl!.Trim().TrimEnd('/');
        return normalized[DbPrefix.Length..];
    }

    public static bool IsBlogPostUrl(string? dbUrl)
    {
        if (string.IsNullOrWhiteSpace(dbUrl))
        {
            return false;
        }

        var normalized = dbUrl.Trim().TrimEnd('/');
        if (!normalized.StartsWith(DbPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var slug = normalized[DbPrefix.Length..];
        return slug.Length > 0 && !slug.Contains('/');
    }

    public static string BuildPath(string? dbUrl, string languageCode)
    {
        var slug = ToSlug(dbUrl);
        if (slug is null)
        {
            return dbUrl ?? "#";
        }

        var encoded = Uri.EscapeDataString(slug);
        return string.IsNullOrWhiteSpace(languageCode)
            ? $"/Blog/{encoded}"
            : $"/Blog/{encoded}/{languageCode}";
    }

    private static string DecodeSlug(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }
        catch (UriFormatException)
        {
            return value;
        }
    }
}
