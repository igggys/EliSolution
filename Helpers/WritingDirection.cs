namespace Helpers;

using System.Globalization;

public static class WritingDirection
{
    public const string LeftToRight = "ltr";
    public const string RightToLeft = "rtl";

    /// <summary>
    /// Returns HTML <c>dir</c> value (<c>rtl</c> / <c>ltr</c>) for the given culture or language code.
    /// </summary>
    /// <param name="cultureOrLanguageCode">e.g. <c>he</c>, <c>he-IL</c>, <c>en</c>, <c>ru-RU</c>.</param>
    public static string GetHtmlDir(string? cultureOrLanguageCode)
        => IsRightToLeft(cultureOrLanguageCode) ? RightToLeft : LeftToRight;

    /// <summary>
    /// Returns whether the culture writes right-to-left.
    /// </summary>
    public static bool IsRightToLeft(string? cultureOrLanguageCode)
    {
        if (string.IsNullOrWhiteSpace(cultureOrLanguageCode))
        {
            return false;
        }

        try
        {
            return CultureInfo.GetCultureInfo(cultureOrLanguageCode.Trim()).TextInfo.IsRightToLeft;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
