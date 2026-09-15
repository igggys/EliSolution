using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Auth;
using DataManager.AdministratorSite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Models.Auth;

namespace AdministratorGateWay.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthDataManager _authDataManager;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthDataManager authDataManager,
        TokenService tokenService,
        ILogger<AuthController> logger)
    {
        _authDataManager = authDataManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized();
        }

        var user = await _authDataManager.ValidateLoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (user is null)
        {
            _logger.LogInformation("Failed login for {Email}", request.Email);
            return Unauthorized();
        }

        var (accessToken, expiresAt) = _tokenService.CreateAccessToken(user);
        _logger.LogInformation("Admin {Email} signed in", user.Email);

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            DisplayName = user.DisplayName,
            Email = user.Email
        });
    }

    [HttpGet("me")]
    public async Task<ActionResult<AdminProfileResponse>> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await _authDataManager.GetAdminUserByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new AdminProfileResponse
        {
            Email = user.Email,
            DisplayName = user.DisplayName
        });
    }

    [HttpPut("profile")]
    public async Task<ActionResult<AdminProfileResponse>> UpdateProfile(
        [FromBody] UpdateAdminProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequest();
        }

        var user = await _authDataManager.GetAdminUserByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        string? passwordHash = null;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var hasher = new PasswordHasher<AdminUser>();
            passwordHash = hasher.HashPassword(user, request.Password);
        }

        await _authDataManager.UpdateAdminUserAsync(
            userId.Value,
            request.Email.Trim(),
            request.DisplayName.Trim(),
            passwordHash,
            cancellationToken);

        _logger.LogInformation("Admin {Id} updated profile", userId.Value);

        return Ok(new AdminProfileResponse
        {
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim()
        });
    }

    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) ? id : null;
    }
}
