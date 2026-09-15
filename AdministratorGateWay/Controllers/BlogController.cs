using AdministratorGateWay.Infrastructure;
using AdministratorGateWay.Models;
using Microsoft.AspNetCore.Mvc;
using Models.AdministratorSite;

namespace AdministratorGateWay.Controllers;

[ApiController]
[Route("api/blog")]
public class BlogController : ControllerBase
{
    private readonly DataManager.AdministratorSite.DataManager _dataManager;
    private readonly BlogImageStorage _blogImageStorage;
    private readonly ILogger<BlogController> _logger;

    public BlogController(
        DataManager.AdministratorSite.DataManager dataManager,
        BlogImageStorage blogImageStorage,
        ILogger<BlogController> logger)
    {
        _dataManager = dataManager;
        _blogImageStorage = blogImageStorage;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BlogPost>>> GetAll(
        [FromQuery] byte? languageId,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var posts = await _dataManager.GetAllBlogPostsAsync(languageId, activeOnly, cancellationToken);
        return Ok(posts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BlogPost>> GetById(int id, CancellationToken cancellationToken)
    {
        var post = await _dataManager.GetBlogPostByIdAsync(id, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        return Ok(post);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<BlogPost>> Create(
        [FromForm] CreateBlogPostRequest request,
        CancellationToken cancellationToken)
    {
        var body = ArticleHtml.ExtractInnerBody(request.Body ?? string.Empty);
        if (request.LanguageId == 0
            || string.IsNullOrWhiteSpace(request.Url)
            || string.IsNullOrWhiteSpace(request.Name)
            || request.Image is null
            || string.IsNullOrWhiteSpace(body))
        {
            return BadRequest("Title, URL, cover image, and article body are required.");
        }

        var savedImageNames = new List<string>();
        try
        {
            var imageFileName = await _blogImageStorage.SaveCoverAsync(request.Image, request.Url, cancellationToken);
            savedImageNames.Add(imageFileName);

            var rewritten = await _blogImageStorage.RewriteBodyAsync(body, request.Url, cancellationToken);
            body = rewritten.Body;
            savedImageNames.AddRange(rewritten.FileNames);

            var post = new BlogPost
            {
                LanguageId = request.LanguageId,
                Url = request.Url,
                Name = request.Name,
                Body = body,
                ImageUrl = imageFileName,
                IsPublished = request.IsPublished,
                IsFeatured = request.IsFeatured,
                IsActive = request.IsActive,
                SortOrder = request.SortOrder,
                PublishedAt = ToUtc(request.PublishedAt),
                SeoTitle = request.SeoTitle,
                MetaDescription = request.MetaDescription,
                MetaRobots = request.MetaRobots,
                OgTitle = request.OgTitle,
                OgDescription = request.OgDescription,
                OgImage = request.OgImage,
                OgType = request.OgType,
                OgUrl = request.OgUrl
            };

            post.Id = await _dataManager.InsertBlogPostAsync(post, cancellationToken);
            _logger.LogInformation("Created blog post {Id} ({Url})", post.Id, post.Url);
            return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
        }
        catch (InvalidOperationException ex)
        {
            await DeleteSavedImagesAsync(savedImageNames, CancellationToken.None);
            return BadRequest(ex.Message);
        }
        catch
        {
            await DeleteSavedImagesAsync(savedImageNames, CancellationToken.None);
            throw;
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var post = await _dataManager.GetBlogPostByIdAsync(id, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        var deleted = await _dataManager.DeleteBlogPostAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        var imageNames = _blogImageStorage.CollectReferencedFiles(post.ImageUrl, post.Body);
        await DeleteSavedImagesAsync(imageNames, cancellationToken);
        _logger.LogInformation("Deleted blog post {Id} ({Url})", post.Id, post.Url);
        return NoContent();
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<BlogPost>> Update(
        int id,
        [FromForm] UpdateBlogPostRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _dataManager.GetBlogPostByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var body = ArticleHtml.ExtractInnerBody(request.Body ?? string.Empty);
        if (request.LanguageId == 0
            || string.IsNullOrWhiteSpace(request.Url)
            || string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(body))
        {
            return BadRequest("Title, URL, and article body are required.");
        }

        var savedImageNames = new List<string>();
        try
        {
            var imageFileName = existing.ImageUrl;
            if (request.Image is not null)
            {
                imageFileName = await _blogImageStorage.SaveCoverAsync(request.Image, request.Url, cancellationToken);
                savedImageNames.Add(imageFileName);
            }

            var rewritten = await _blogImageStorage.RewriteBodyAsync(body, request.Url, cancellationToken);
            body = rewritten.Body;
            savedImageNames.AddRange(rewritten.FileNames);

            var post = new BlogPost
            {
                Id = id,
                LanguageId = request.LanguageId,
                Url = request.Url,
                Name = request.Name,
                Body = body,
                ImageUrl = imageFileName,
                IsPublished = request.IsPublished,
                IsFeatured = request.IsFeatured,
                IsActive = request.IsActive,
                SortOrder = request.SortOrder,
                PublishedAt = ToUtc(request.PublishedAt),
                SeoTitle = request.SeoTitle,
                MetaDescription = request.MetaDescription,
                MetaRobots = request.MetaRobots,
                OgTitle = request.OgTitle,
                OgDescription = request.OgDescription,
                OgImage = request.OgImage,
                OgType = request.OgType,
                OgUrl = request.OgUrl
            };

            var updated = await _dataManager.UpdateBlogPostAsync(post, cancellationToken);
            if (!updated)
            {
                await DeleteSavedImagesAsync(savedImageNames, CancellationToken.None);
                return NotFound();
            }

            var kept = new HashSet<string>(
                _blogImageStorage.CollectReferencedFiles(imageFileName, body),
                StringComparer.OrdinalIgnoreCase);
            var previous = _blogImageStorage.CollectReferencedFiles(existing.ImageUrl, existing.Body);
            var orphans = previous.Where(name => !kept.Contains(name));
            await DeleteSavedImagesAsync(orphans, cancellationToken);

            var saved = await _dataManager.GetBlogPostByIdAsync(id, cancellationToken);
            _logger.LogInformation("Updated blog post {Id} ({Url})", id, post.Url);
            return Ok(saved ?? post);
        }
        catch (InvalidOperationException ex)
        {
            await DeleteSavedImagesAsync(savedImageNames, CancellationToken.None);
            return BadRequest(ex.Message);
        }
        catch
        {
            await DeleteSavedImagesAsync(savedImageNames, CancellationToken.None);
            throw;
        }
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        var publishedAt = value.GetValueOrDefault();
        return publishedAt.Kind switch
        {
            DateTimeKind.Utc => publishedAt,
            DateTimeKind.Local => publishedAt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(publishedAt, DateTimeKind.Local).ToUniversalTime()
        };
    }

    private async Task DeleteSavedImagesAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken)
    {
        foreach (var fileName in fileNames)
        {
            await _blogImageStorage.DeleteAsync(fileName, cancellationToken);
        }
    }
}
