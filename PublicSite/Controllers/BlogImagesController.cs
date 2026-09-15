using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Options;

namespace PublicSite.Controllers;

[AllowAnonymous]
public class BlogImagesController(
    DataManager.PublicSite.DataManager dataManager,
    IWebHostEnvironment environment,
    ILogger<BlogImagesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Get(string fileName, CancellationToken cancellationToken)
    {
        if (!BlogImageFileName.TryNormalize(fileName, out var normalized))
        {
            return BadRequest();
        }

        try
        {
            var image = await dataManager.GetBlogImageByFileNameAsync(normalized, cancellationToken);
            if (image is { Content.Length: > 0 })
            {
                Response.Headers.CacheControl = "public, max-age=31536000";
                return File(image.Content, image.ContentType);
            }
        }
        catch (DataManager.DataManagerException ex)
        {
            logger.LogError(ex, "Failed to load blog image {FileName} from the database", normalized);
        }

        var diskPath = Path.Combine(environment.WebRootPath, "images", "blog", normalized);
        if (System.IO.File.Exists(diskPath))
        {
            Response.Headers.CacheControl = "public, max-age=31536000";
            return PhysicalFile(diskPath, ContentTypeFor(normalized));
        }

        return NotFound();
    }

    private static string ContentTypeFor(string fileName) => Path.GetExtension(fileName) switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "image/png"
    };
}
