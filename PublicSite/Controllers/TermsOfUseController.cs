using DataManager;
using Microsoft.AspNetCore.Mvc;

namespace PublicSite.Controllers;

public class TermsOfUseController : Controller
{
    private readonly DataManager.PublicSite.DataManager _dataManager;
    private readonly ILogger<TermsOfUseController> _logger;

    public TermsOfUseController(
        DataManager.PublicSite.DataManager dataManager,
        ILogger<TermsOfUseController> logger)
    {
        _dataManager = dataManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? lang, CancellationToken cancellationToken)
    {
        try
        {
            var page = await _dataManager.GetTermsOfUsePageAsync(lang, cancellationToken);

            if (page.TermsOfUse is null)
            {
                return NotFound();
            }

            return View(page);
        }
        catch (DataManagerException ex)
        {
            _logger.LogError(ex, "Failed to load terms of use page for language {Lang}", lang);
            throw;
        }
    }
}
