using DataManager;
using Helpers;
using Microsoft.AspNetCore.Mvc;

namespace PublicSite.Controllers;

public class BlogController : Controller
{
    private readonly DataManager.PublicSite.DataManager _dataManager;
    private readonly ILogger<BlogController> _logger;

    public BlogController(
        DataManager.PublicSite.DataManager dataManager,
        ILogger<BlogController> logger)
    {
        _dataManager = dataManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? lang, int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var blogPage = await _dataManager.GetBlogPageAsync(lang, page, cancellationToken: cancellationToken);
            return View(blogPage);
        }
        catch (DataManagerException ex)
        {
            _logger.LogError(ex, "Failed to load blog page for language {Lang}", lang);
            throw;
        }
    }

    public async Task<IActionResult> Article(string? slug, string? lang, CancellationToken cancellationToken)
    {
        var blogPostUrl = BlogRoutes.ToDbUrl(slug);
        if (blogPostUrl is null)
        {
            return NotFound();
        }

        try
        {
            var page = await _dataManager.GetBlogPostPageAsync(blogPostUrl, lang, cancellationToken);

            if (page.Post is null)
            {
                return NotFound();
            }

            return View(page);
        }
        catch (DataManagerException ex)
        {
            _logger.LogError(ex, "Failed to load blog article {Slug}/{Lang}", slug, lang);
            throw;
        }
    }
}
