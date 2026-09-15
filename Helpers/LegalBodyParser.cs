namespace Helpers;

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Splits a legal-page Body HTML blob (intro, one or more lists, date line) for the Reyou-style layout.
/// </summary>
public static class LegalBodyParser
{
    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.CultureInvariant);

    public static LegalDocument Parse(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return LegalDocument.Empty;
        }

        var source = html.Trim();
        var intro = new StringBuilder();
        var drafts = new List<SectionDraft>();
        string? afterword = null;
        var i = 0;

        while (i < source.Length)
        {
            while (i < source.Length && char.IsWhiteSpace(source[i]))
            {
                i++;
            }

            if (i >= source.Length)
            {
                break;
            }

            if (source[i] != '<')
            {
                var nextTag = source.IndexOf('<', i);
                var text = nextTag < 0 ? source[i..] : source[i..nextTag];
                AppendLoose(text, intro, drafts);
                i = nextTag < 0 ? source.Length : nextTag;
                continue;
            }

            var tagName = ReadTagName(source, i);
            if (tagName is null)
            {
                i++;
                continue;
            }

            if (tagName is "ol" or "ul")
            {
                if (!TryReadElement(source, i, tagName, out var close, out var inner))
                {
                    break;
                }

                if (tagName == "ol")
                {
                    foreach (var itemHtml in ParseTopLevelItemHtml(inner))
                    {
                        if (TryCreateHeading(itemHtml, out var title, out var body))
                        {
                            var draft = new SectionDraft { Title = title };
                            if (!string.IsNullOrWhiteSpace(body))
                            {
                                draft.Body.Append(body);
                            }

                            drafts.Add(draft);
                        }
                        else
                        {
                            AppendLoose(WrapLooseItem(itemHtml), intro, drafts);
                        }
                    }
                }
                else
                {
                    var closeLength = 2 + tagName.Length + 1;
                    AppendLoose(source[i..(close + closeLength)], intro, drafts);
                }

                i = close + 2 + tagName.Length + 1;
                continue;
            }

            if (!TryReadElement(source, i, tagName, out var elementClose, out _))
            {
                AppendLoose(source[i..], intro, drafts);
                break;
            }

            var blockCloseLength = 2 + tagName.Length + 1;
            var block = source[i..(elementClose + blockCloseLength)];
            if (IsUpdatedDateBlock(block))
            {
                afterword = block.Trim();
            }
            else
            {
                AppendLoose(block, intro, drafts);
            }

            i = elementClose + blockCloseLength;
        }

        var sections = new List<LegalSection>(drafts.Count);
        for (var n = 0; n < drafts.Count; n++)
        {
            var body = drafts[n].Body.ToString().Trim();
            sections.Add(new LegalSection
            {
                Number = n + 1,
                Title = drafts[n].Title,
                BodyHtml = string.IsNullOrWhiteSpace(body) ? null : body
            });
        }

        var introHtml = intro.ToString().Trim();
        return new LegalDocument
        {
            IntroHtml = string.IsNullOrWhiteSpace(introHtml) ? null : introHtml,
            AfterwordHtml = afterword,
            Sections = sections
        };
    }

    private static string WrapLooseItem(string itemHtml)
    {
        var trimmed = itemHtml.Trim();
        if (trimmed.Length == 0)
        {
            return trimmed;
        }

        if (trimmed[0] == '<')
        {
            return trimmed;
        }

        return "<p>" + trimmed + "</p>";
    }

    private static void AppendLoose(string html, StringBuilder intro, List<SectionDraft> drafts)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return;
        }

        if (drafts.Count == 0)
        {
            intro.Append(html);
            return;
        }

        drafts[^1].Body.Append(html);
    }

    private static bool IsUpdatedDateBlock(string html)
    {
        var text = DecodeText(StripTags(html));
        return text.Contains("עודכן לאחרונה", StringComparison.Ordinal)
            || text.Contains("עודכנה לאחרונה", StringComparison.Ordinal);
    }

    private static bool TryReadElement(string html, int openIndex, string tagName, out int closeIndex, out string inner)
    {
        closeIndex = -1;
        inner = string.Empty;
        var openEnd = html.IndexOf('>', openIndex);
        if (openEnd < 0)
        {
            return false;
        }

        var innerStart = openEnd + 1;
        closeIndex = FindMatchingClose(html, innerStart, tagName);
        if (closeIndex < 0)
        {
            return false;
        }

        inner = html[innerStart..closeIndex];
        return true;
    }

    private static string? ReadTagName(string html, int openIndex)
    {
        if (openIndex + 1 >= html.Length || html[openIndex] != '<')
        {
            return null;
        }

        var start = openIndex + 1;
        if (html[start] is '/' or '!' or '?')
        {
            return null;
        }

        var end = start;
        while (end < html.Length && char.IsLetter(html[end]))
        {
            end++;
        }

        return end == start ? null : html[start..end].ToLowerInvariant();
    }

    private static IReadOnlyList<string> ParseTopLevelItemHtml(string inner)
    {
        var items = new List<string>();
        var depth = 0;
        var itemStart = -1;
        var i = 0;

        while (i < inner.Length)
        {
            if (IsCloseTag(inner, i, "li"))
            {
                if (depth == 1 && itemStart >= 0)
                {
                    items.Add(inner[itemStart..i].Trim());
                    itemStart = -1;
                }

                if (depth > 0)
                {
                    depth--;
                }

                i = SkipTag(inner, i);
                continue;
            }

            if (IsOpenTag(inner, i, "li"))
            {
                if (depth == 0)
                {
                    i = SkipTag(inner, i);
                    itemStart = i;
                    depth = 1;
                    continue;
                }

                depth++;
                i = SkipTag(inner, i);
                continue;
            }

            i++;
        }

        return items;
    }

    private static bool TryCreateHeading(string itemHtml, out string title, out string? body)
    {
        title = string.Empty;
        body = null;
        var strongStart = IndexOfOpenTag(itemHtml, "strong", 0);
        if (strongStart < 0)
        {
            return false;
        }

        var before = DecodeText(StripTags(itemHtml[..strongStart]));
        if (before.Length > 0)
        {
            return false;
        }

        var strongOpenEnd = itemHtml.IndexOf('>', strongStart);
        var strongClose = IndexOfCloseTag(itemHtml, "strong", strongOpenEnd + 1);
        if (strongOpenEnd < 0 || strongClose < 0)
        {
            return false;
        }

        title = DecodeText(StripTags(itemHtml[(strongOpenEnd + 1)..strongClose])).Trim();
        if (title.Length == 0)
        {
            return false;
        }

        var remainder = (itemHtml[..strongStart] + itemHtml[(strongClose + "</strong>".Length)..]).Trim();
        body = string.IsNullOrWhiteSpace(remainder) ? null : remainder;
        return true;
    }

    private static int FindMatchingClose(string html, int innerStart, string tag)
    {
        var depth = 1;
        var i = innerStart;

        while (i < html.Length)
        {
            if (IsCloseTag(html, i, tag))
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }

                i = SkipTag(html, i);
                continue;
            }

            if (IsOpenTag(html, i, tag))
            {
                depth++;
                i = SkipTag(html, i);
                continue;
            }

            i++;
        }

        return -1;
    }

    private static int IndexOfOpenTag(string html, string tag, int start)
    {
        var needle = "<" + tag;
        var i = start;

        while (i < html.Length)
        {
            var idx = html.IndexOf(needle, i, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                return -1;
            }

            if (IsOpenTag(html, idx, tag))
            {
                return idx;
            }

            i = idx + 1;
        }

        return -1;
    }

    private static int IndexOfCloseTag(string html, string tag, int start)
    {
        var needle = "</" + tag + ">";
        return html.IndexOf(needle, start, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOpenTag(string html, int index, string tag)
    {
        if (index < 0 || index + tag.Length + 1 >= html.Length)
        {
            return false;
        }

        if (html[index] != '<')
        {
            return false;
        }

        if (!html.AsSpan(index + 1).StartsWith(tag, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var after = html[index + 1 + tag.Length];
        return after is '>' or ' ' or '\t' or '\n' or '\r' or '/';
    }

    private static bool IsCloseTag(string html, int index, string tag)
    {
        var needle = "</" + tag + ">";
        if (index < 0 || index + needle.Length > html.Length)
        {
            return false;
        }

        return html.AsSpan(index).StartsWith(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static int SkipTag(string html, int index)
    {
        var end = html.IndexOf('>', index);
        return end < 0 ? html.Length : end + 1;
    }

    private static string StripTags(string html)
        => TagRegex.Replace(html, string.Empty);

    private static string DecodeText(string text)
        => WebUtility.HtmlDecode(text).Trim();

    private sealed class SectionDraft
    {
        public required string Title { get; init; }

        public StringBuilder Body { get; } = new();
    }
}

public sealed class LegalDocument
{
    public static LegalDocument Empty { get; } = new();

    public string? IntroHtml { get; init; }

    public string? AfterwordHtml { get; init; }

    public IReadOnlyList<LegalSection> Sections { get; init; } = [];

    public bool HasStructuredSections => Sections.Count >= 2;
}

public sealed class LegalSection
{
    public int Number { get; init; }

    public required string Title { get; init; }

    public string? BodyHtml { get; init; }

    public string NumberLabel => Number.ToString("00", CultureInfo.InvariantCulture);
}
