namespace Helpers;

public static class SiteRoutes
{
    public static string BuildMenuPath(string? dbUrl, string languageCode)
    {
        if (PracticeAreaRoutes.IsPracticeAreaUrl(dbUrl))
        {
            return PracticeAreaRoutes.BuildPath(dbUrl, languageCode);
        }

        if (IsHomeUrl(dbUrl))
        {
            return string.IsNullOrWhiteSpace(languageCode)
                ? "/"
                : $"/Home/{languageCode}";
        }

        if (IsAboutUrl(dbUrl))
        {
            return string.IsNullOrWhiteSpace(languageCode)
                ? "/About"
                : $"/About/{languageCode}";
        }

        if (IsBlogUrl(dbUrl))
        {
            return string.IsNullOrWhiteSpace(languageCode)
                ? "/Blog"
                : $"/Blog/{languageCode}";
        }

        if (BlogRoutes.IsBlogPostUrl(dbUrl))
        {
            return BlogRoutes.BuildPath(dbUrl, languageCode);
        }

        if (IsPrivacyPolicyUrl(dbUrl))
        {
            return BuildPrivacyPolicyPath(languageCode);
        }

        if (IsTermsOfUseUrl(dbUrl))
        {
            return BuildTermsOfUsePath(languageCode);
        }

        return string.IsNullOrWhiteSpace(dbUrl) ? "#" : dbUrl;
    }

    public static string BuildPrivacyPolicyPath(string languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "/PrivacyPolicy"
            : $"/PrivacyPolicy/{languageCode}";
    }

    public static string BuildTermsOfUsePath(string languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "/TermsOfUse"
            : $"/TermsOfUse/{languageCode}";
    }

    public static bool IsAboutUrl(string? dbUrl)
    {
        if (string.IsNullOrWhiteSpace(dbUrl))
        {
            return false;
        }

        var normalized = dbUrl.Trim().TrimEnd('/');
        return string.Equals(normalized, "/about", StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, "about", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsBlogUrl(string? dbUrl)
    {
        if (string.IsNullOrWhiteSpace(dbUrl))
        {
            return false;
        }

        var normalized = dbUrl.Trim().TrimEnd('/');
        return string.Equals(normalized, "/blog", StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, "blog", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPrivacyPolicyUrl(string? dbUrl)
    {
        if (string.IsNullOrWhiteSpace(dbUrl))
        {
            return false;
        }

        var normalized = dbUrl.Trim().TrimEnd('/');
        return string.Equals(normalized, "/privacy-policy", StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, "privacy-policy", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTermsOfUseUrl(string? dbUrl)
    {
        if (string.IsNullOrWhiteSpace(dbUrl))
        {
            return false;
        }

        var normalized = dbUrl.Trim().TrimEnd('/');
        return string.Equals(normalized, "/terms-of-use", StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, "terms-of-use", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsHomeUrl(string? dbUrl)
    {
        if (string.IsNullOrWhiteSpace(dbUrl))
        {
            return false;
        }

        var normalized = dbUrl.Trim().TrimEnd('/');
        return normalized.Length == 0
               || string.Equals(normalized, "/home", StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, "home", StringComparison.OrdinalIgnoreCase);
    }
}
