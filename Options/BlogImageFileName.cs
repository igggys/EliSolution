using System.Text.RegularExpressions;

namespace Options;

public static class BlogImageFileName
{
    private static readonly Regex ValidName = new(
        "^[a-z0-9-]+\\.(png|jpg|jpeg|gif|webp)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryNormalize(string? fileName, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var name = Path.GetFileName(fileName.Trim()).ToLowerInvariant();
        if (!ValidName.IsMatch(name))
        {
            return false;
        }

        normalized = name;
        return true;
    }
}
