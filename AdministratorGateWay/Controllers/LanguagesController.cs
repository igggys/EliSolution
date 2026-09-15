using Microsoft.AspNetCore.Mvc;
using Models.AdministratorSite;

namespace AdministratorGateWay.Controllers;

[ApiController]
[Route("api/languages")]
public class LanguagesController : ControllerBase
{
    private readonly DataManager.AdministratorSite.DataManager _dataManager;
    private readonly ILogger<LanguagesController> _logger;

    public LanguagesController(
        DataManager.AdministratorSite.DataManager dataManager,
        ILogger<LanguagesController> logger)
    {
        _dataManager = dataManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Language>>> GetAll(
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var languages = await _dataManager.GetAllLanguagesAsync(activeOnly, cancellationToken);
        return Ok(languages);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Language>> GetById(byte id, CancellationToken cancellationToken)
    {
        var languages = await _dataManager.GetAllLanguagesAsync(activeOnly: false, cancellationToken);
        var language = languages.FirstOrDefault(item => item.Id == id);
        if (language is null)
        {
            return NotFound();
        }

        return Ok(language);
    }

    [HttpPost]
    public async Task<ActionResult<Language>> Create(
        [FromBody] Language language,
        CancellationToken cancellationToken)
    {
        language.Id = await _dataManager.InsertLanguageAsync(language, cancellationToken);
        _logger.LogInformation("Created language {Code} with id {Id}", language.Code, language.Id);
        return CreatedAtAction(nameof(GetById), new { id = language.Id }, language);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        byte id,
        [FromBody] Language language,
        CancellationToken cancellationToken)
    {
        language.Id = id;
        await _dataManager.UpdateLanguageAsync(language, cancellationToken);
        _logger.LogInformation("Updated language {Id} ({Code})", language.Id, language.Code);
        return NoContent();
    }
}
