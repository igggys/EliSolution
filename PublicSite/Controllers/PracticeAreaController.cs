using DataManager;
using Helpers;
using Microsoft.AspNetCore.Mvc;

namespace PublicSite.Controllers;

public class PracticeAreaController : Controller
{
    private readonly DataManager.PublicSite.DataManager _dataManager;
    private readonly ILogger<PracticeAreaController> _logger;

    public PracticeAreaController(
        DataManager.PublicSite.DataManager dataManager,
        ILogger<PracticeAreaController> logger)
    {
        _dataManager = dataManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? section, string? lang, CancellationToken cancellationToken)
    {
        var practiceAreaUrl = PracticeAreaRoutes.ToDbUrl(section);
        if (practiceAreaUrl is null)
        {
            return NotFound();
        }

        try
        {
            var page = await _dataManager.GetPracticeAreaPageAsync(practiceAreaUrl, lang, cancellationToken);

            if (page.PracticeArea is null)
            {
                return NotFound();
            }

            return View(page);
        }
        catch (DataManagerException ex)
        {
            _logger.LogError(ex, "Failed to load practice area page {Section}/{Lang}", section, lang);
            throw;
        }
    }
}
