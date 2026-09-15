using DataManager;
using Microsoft.AspNetCore.Mvc;

namespace PublicSite.Controllers;

public class AboutController : Controller
{
    private readonly DataManager.PublicSite.DataManager _dataManager;
    private readonly ILogger<AboutController> _logger;

    public AboutController(
        DataManager.PublicSite.DataManager dataManager,
        ILogger<AboutController> logger)
    {
        _dataManager = dataManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? lang, CancellationToken cancellationToken)
    {
        try
        {
            var page = await _dataManager.GetAboutPageAsync(lang, cancellationToken);

            if (page.About is null)
            {
                return NotFound();
            }

            return View(page);
        }
        catch (DataManagerException ex)
        {
            _logger.LogError(ex, "Failed to load about page for language {Lang}", lang);
            throw;
        }
    }
}
