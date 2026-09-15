using System.Text.RegularExpressions;

namespace AdministratorGateWay.Infrastructure;

internal static class ArticleHtml
{
    private static readonly Regex BodyInner = new(
        @"<body\b[^>]*>([\s\S]*)</body>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string ExtractInnerBody(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        var match = BodyInner.Match(html);
        return match.Success ? match.Groups[1].Value.Trim() : html.Trim();
    }
}
