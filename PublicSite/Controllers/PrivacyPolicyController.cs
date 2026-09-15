using DataManager;
using Microsoft.AspNetCore.Mvc;

namespace PublicSite.Controllers;

public class PrivacyPolicyController : Controller
{
    private readonly DataManager.PublicSite.DataManager _dataManager;
    private readonly ILogger<PrivacyPolicyController> _logger;

    public PrivacyPolicyController(
        DataManager.PublicSite.DataManager dataManager,
        ILogger<PrivacyPolicyController> logger)
    {
        _dataManager = dataManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? lang, CancellationToken cancellationToken)
    {
        try
        {
            var page = await _dataManager.GetPrivacyPolicyPageAsync(lang, cancellationToken);

            if (page.PrivacyPolicy is null)
            {
                return NotFound();
            }

            return View(page);
        }
        catch (DataManagerException ex)
        {
            _logger.LogError(ex, "Failed to load privacy policy page for language {Lang}", lang);
            throw;
        }
    }
}
