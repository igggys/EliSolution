using DataManager;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PublicSite.Controllers;

public class ErrorController : Controller
{
    private readonly DataManager.PublicSite.DataManager _dataManager;
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(
        DataManager.PublicSite.DataManager dataManager,
        ILogger<ErrorController> logger)
    {
        _dataManager = dataManager;
        _logger = logger;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Status(int statusCode, CancellationToken cancellationToken)
    {
        if (statusCode != StatusCodes.Status404NotFound)
        {
            return StatusCode(statusCode);
        }

        Response.StatusCode = StatusCodes.Status404NotFound;

        try
        {
            var languageCode = ResolveLanguageCode();
            var page = await _dataManager.GetNotFoundPageAsync(languageCode, cancellationToken);
            return View("NotFound", page);
        }
        catch (DataManagerException ex)
        {
            _logger.LogError(ex, "Failed to load 404 page");
            throw;
        }
    }

    private string? ResolveLanguageCode()
    {
        var originalPath = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath
            ?? Request.Path.Value;

        if (string.IsNullOrWhiteSpace(originalPath))
        {
            return null;
        }

        var segments = originalPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 ? null : segments[^1];
    }
}
