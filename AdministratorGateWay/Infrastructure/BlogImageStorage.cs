using System.Text.RegularExpressions;
using Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace AdministratorGateWay.Infrastructure;

public sealed class BlogImageStorage(DataManager.AdministratorSite.DataManager dataManager)
{
    public const int TargetWidth = 900;
    public const int TargetHeight = 603;
    public const int MaxInlineBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".gif" };

    private static readonly Regex DataImageSrc = new(
        "(?<=<img\\b[^>]*\\bsrc\\s*=\\s*[\"']?)data:image/(png|jpeg|jpg|gif|webp);base64,([A-Za-z0-9+/=\\s]+)(?=[\"'\\s>])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public async Task<string> SaveCoverAsync(IFormFile image, string url, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(image);

        var extension = Path.GetExtension(image.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Image must be png, jpg, jpeg, webp, or gif.");
        }

        byte[] pngBytes;
        try
        {
            await using var upload = image.OpenReadStream();
            using var processed = await Image.LoadAsync(upload, cancellationToken);
            processed.Mutate(operation => operation.Resize(new ResizeOptions
            {
                Size = new Size(TargetWidth, TargetHeight),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            await using var output = new MemoryStream();
            await processed.SaveAsPngAsync(output, new PngEncoder(), cancellationToken);
            pngBytes = output.ToArray();
        }
        catch (UnknownImageFormatException)
        {
            throw new InvalidOperationException("The uploaded file is not a valid image.");
        }

        var fileName = await AllocateFileNameAsync(BuildSlug(url) + ".png", cancellationToken);
        await dataManager.InsertBlogImageAsync(fileName, "image/png", pngBytes, cancellationToken);
        return fileName;
    }

    public async Task<(string Body, IReadOnlyList<string> FileNames)> RewriteBodyAsync(
        string html,
        string url,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return (html, []);
        }

        var matches = DataImageSrc.Matches(html);
        if (matches.Count == 0)
        {
            return (html, []);
        }

        var slug = BuildSlug(url);
        var saved = new List<string>();
        var updated = html;
        var inlineIndex = 0;

        foreach (Match match in matches.Cast<Match>().Reverse())
        {
            var subtype = match.Groups[1].Value.ToLowerInvariant();
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(Regex.Replace(match.Groups[2].Value, @"\s+", string.Empty));
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("An image in the article body is not valid.");
            }

            if (bytes.Length == 0 || bytes.Length > MaxInlineBytes)
            {
                throw new InvalidOperationException("Each image in the article body must be 2 MB or smaller.");
            }

            inlineIndex++;
            var extension = ExtensionForSubtype(subtype);
            var contentType = ContentTypeForSubtype(subtype);
            var fileName = await AllocateFileNameAsync($"{slug}-{inlineIndex}{extension}", cancellationToken);
            await dataManager.InsertBlogImageAsync(fileName, contentType, bytes, cancellationToken);
            saved.Add(fileName);
            updated = updated.Remove(match.Index, match.Length).Insert(match.Index, "/images/blog/" + fileName);
        }

        return (updated, saved);
    }

    public IReadOnlyList<string> CollectReferencedFiles(string? imageUrl, string? body)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (BlogImageFileName.TryNormalize(imageUrl, out var cover))
        {
            names.Add(cover);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return names.ToList();
        }

        foreach (Match match in Regex.Matches(body, @"/images/blog/([A-Za-z0-9._-]+)", RegexOptions.IgnoreCase))
        {
            if (BlogImageFileName.TryNormalize(match.Groups[1].Value, out var fileName))
            {
                names.Add(fileName);
            }
        }

        return names.ToList();
    }

    public async Task DeleteAsync(string fileName, CancellationToken cancellationToken)
    {
        if (!BlogImageFileName.TryNormalize(fileName, out var normalized))
        {
            return;
        }

        try
        {
            await dataManager.DeleteBlogImageAsync(normalized, cancellationToken);
        }
        catch (DataManager.DataManagerException)
        {
            // Rollback must not hide the original create error.
        }
    }

    private async Task<string> AllocateFileNameAsync(string preferred, CancellationToken cancellationToken)
    {
        if (!BlogImageFileName.TryNormalize(preferred, out var fileName))
        {
            fileName = $"post-{DateTime.UtcNow:yyyyMMddHHmmss}.png";
        }

        if (!await dataManager.BlogImageExistsAsync(fileName, cancellationToken))
        {
            return fileName;
        }

        var stamped =
            $"{Path.GetFileNameWithoutExtension(fileName)}-{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(fileName)}";
        return BlogImageFileName.TryNormalize(stamped, out var unique)
            ? unique
            : $"post-{Guid.NewGuid():N}.png";
    }

    private static string BuildSlug(string url)
    {
        var slug = url.Trim().Trim('/').Replace('\\', '/');
        var slash = slug.LastIndexOf('/');
        if (slash >= 0)
        {
            slug = slug[(slash + 1)..];
        }

        slug = Regex.Replace(slug.ToLowerInvariant(), "[^a-z0-9-]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "post" : slug;
    }

    private static string ExtensionForSubtype(string subtype) => subtype switch
    {
        "jpeg" or "jpg" => ".jpg",
        "gif" => ".gif",
        "webp" => ".webp",
        _ => ".png"
    };

    private static string ContentTypeForSubtype(string subtype) => subtype switch
    {
        "jpeg" or "jpg" => "image/jpeg",
        "gif" => "image/gif",
        "webp" => "image/webp",
        _ => "image/png"
    };
}
