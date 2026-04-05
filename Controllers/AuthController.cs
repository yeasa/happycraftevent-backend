using HappyCraftEvent.Contracts.DTOs.Auth;
using HappyCraftEvent.Contracts.StatusCodes;
using HappyCraftEvent.Helper.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HappyCraftEvent.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    private string? GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var value))
            return value.ToString().Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            if (request is null)
                return BadRequest("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required.");

            var ipAddress = GetClientIpAddress();
            var (statusCode, response) = await _authService.LoginAsync(request, ipAddress);

            return statusCode switch
            {
                HappyCraftStatusCode.OK => Ok(response),
                HappyCraftStatusCode.INVALID_REQUEST => BadRequest("Invalid email or password format."),
                HappyCraftStatusCode.UNAUTHORIZED => Unauthorized("Invalid credentials."),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest("Refresh token is required.");

            var ipAddress = GetClientIpAddress();
            var (statusCode, response) = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

            return statusCode switch
            {
                HappyCraftStatusCode.OK => Ok(response),
                HappyCraftStatusCode.INVALID_REQUEST => BadRequest("Refresh token is required."),
                HappyCraftStatusCode.RECORD_NOT_FOUND => NotFound("Token not found."),
                HappyCraftStatusCode.UNAUTHORIZED => Unauthorized("Refresh token is invalid, expired, or revoked."),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpGet("verify")]
    [Authorize]
    public async Task<IActionResult> Verify()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("User ID not found in token.");

            var (statusCode, response) = await _authService.GetCurrentUserAsync(userId);

            return statusCode switch
            {
                HappyCraftStatusCode.OK => Ok(response),
                HappyCraftStatusCode.RECORD_NOT_FOUND => NotFound("User not found."),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during verify.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest("Refresh token is required.");

            var ipAddress = GetClientIpAddress();
            var (statusCode, success) = await _authService.RevokeTokenAsync(request.RefreshToken, ipAddress);

            return statusCode switch
            {
                HappyCraftStatusCode.OK => Ok(new { message = "Logged out successfully." }),
                HappyCraftStatusCode.INVALID_REQUEST => BadRequest("Refresh token is required."),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
