using DataManager;
using Microsoft.AspNetCore.Mvc;
using Services.MailService;

namespace PublicSite.Controllers;

public class HomeController : Controller
{
    private readonly DataManager.PublicSite.DataManager _dataManager;
    private readonly IMailService _mailService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        DataManager.PublicSite.DataManager dataManager,
        IMailService mailService,
        ILogger<HomeController> logger)
    {
        _dataManager = dataManager;
        _mailService = mailService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? lang, CancellationToken cancellationToken)
    {
        try
        {
            var page = await _dataManager.GetHomePageAsync(lang, cancellationToken);
            return View(page);
        }
        catch (DataManagerException ex)
        {
            _logger.LogError(ex, "Failed to load home page for language {Lang}", lang);
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(
        string? lang,
        [FromForm] ContactMessage contactMessage,
        [FromForm] bool consent,
        CancellationToken cancellationToken)
    {
        if (!consent
            || string.IsNullOrWhiteSpace(contactMessage.FullName)
            || string.IsNullOrWhiteSpace(contactMessage.Phone)
            || string.IsNullOrWhiteSpace(contactMessage.Email))
        {
            return BadRequest(new { ok = false });
        }

        try
        {
            var page = await _dataManager.GetHomePageAsync(lang, cancellationToken);
            var recipientEmail = page.ContactInfo?.Email;

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                throw new MailServiceException("ContactInfo email is not configured.");
            }

            await _mailService.SendContactMessageAsync(contactMessage, recipientEmail, cancellationToken);
            return Json(new { ok = true });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (MailServiceException ex)
        {
            _logger.LogError(ex, "Failed to send contact form message");
            return StatusCode(StatusCodes.Status500InternalServerError, new { ok = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact form message");
            return StatusCode(StatusCodes.Status500InternalServerError, new { ok = false });
        }
    }
}
