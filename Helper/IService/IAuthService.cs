using HappyCraftEvent.Contracts.DTOs.Auth;
using HappyCraftEvent.Contracts.Enums;

namespace HappyCraftEvent.Helper.IService;

public interface IAuthService
{
    /// <summary>
    /// Authenticates user with email and password, returns tokens and user info.
    /// </summary>
    Task<(int statusCode, LoginResponseDto? response)> LoginAsync(LoginRequestDto request, string? ipAddress = null);

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    Task<(int statusCode, RefreshTokenResponseDto? response)> RefreshTokenAsync(string refreshToken, string? ipAddress = null);

    /// <summary>
    /// Revokes a refresh token (for logout).
    /// </summary>
    Task<(int statusCode, bool success)> RevokeTokenAsync(string refreshToken, string? ipAddress = null);

    /// <summary>
    /// Gets current authenticated user details from claims.
    /// </summary>
    Task<(int statusCode, VerifyResponseDto? response)> GetCurrentUserAsync(Guid userId);
}
