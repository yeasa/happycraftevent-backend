using HappyCraftEvent.Contracts.DTOs.Auth;
using HappyCraftEvent.Contracts.Enums;
using HappyCraftEvent.Contracts.Scopes;
using HappyCraftEvent.Contracts.StatusCodes;
using HappyCraftEvent.DataAccess.IRepository;
using HappyCraftEvent.Helper.IService;
using HappyCraftEvent.Helper.Utilities;

namespace HappyCraftEvent.Helper.Service;

public class AuthService : IAuthService
{
    private readonly IAuthDal _authDal;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IAuthDal authDal, JwtTokenService jwtTokenService, ILogger<AuthService> logger)
    {
        _authDal = authDal;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<(int statusCode, LoginResponseDto? response)> LoginAsync(LoginRequestDto request, string? ipAddress = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return (HappyCraftStatusCode.INVALID_REQUEST, null);

            // Fetch user by email
            var userResult = await _authDal.GetUserByEmailAsync(request.Email);
            if (userResult is null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                return (HappyCraftStatusCode.UNAUTHORIZED, null);
            }

            var (_, userDto, passwordHash) = userResult.Value;

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, passwordHash))
            {
                _logger.LogWarning("Failed login attempt for user: {UserId}", userDto.Id);
                return (HappyCraftStatusCode.UNAUTHORIZED, null);
            }

            // Check status
            if (userDto.Status != UserStatus.Active)
            {
                _logger.LogWarning("Login attempt by non-active user: {UserId} with status {Status}", userDto.Id, userDto.Status);
                return (HappyCraftStatusCode.UNAUTHORIZED, null);
            }

            var scopes = RoleScopeMap.GetScopesForRole(userDto.Role);

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(userDto, scopes);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Store refresh token (hashed)
            var refreshTokenHash = HashToken(refreshToken);
            var expiresAt = DateTime.UtcNow.AddDays(14);
            var (_, storeSuccess) = await _authDal.StoreRefreshTokenAsync(userDto.Id, refreshTokenHash, expiresAt, ipAddress);

            if (!storeSuccess)
            {
                _logger.LogError("Failed to store refresh token for user: {UserId}", userDto.Id);
                return (HappyCraftStatusCode.DB_ERROR, null);
            }

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _jwtTokenService.GetAccessTokenExpirySeconds(),
                User = new UserSummaryDto
                {
                    Id = userDto.Id,
                    Email = userDto.Email,
                    FirstName = userDto.FirstName,
                    LastName = userDto.LastName,
                    Role = userDto.Role,
                    Status = userDto.Status,
                    Scopes = scopes
                }
            };

            _logger.LogInformation("Successful login for user: {UserId}", userDto.Id);
            return (HappyCraftStatusCode.OK, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login.");
            return (HappyCraftStatusCode.INTERNAL_ERROR, null);
        }
    }

    public async Task<(int statusCode, RefreshTokenResponseDto? response)> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return (HappyCraftStatusCode.INVALID_REQUEST, null);

            // Validate refresh token
            var refreshTokenHash = HashToken(refreshToken);
            var (validateStatus, userId, isValid) = await _authDal.ValidateRefreshTokenAsync(refreshTokenHash);

            if (!isValid || userId is null)
                return (validateStatus, null);

            // Fetch user
            var userDto = await _authDal.GetUserByIdAsync(userId.Value);
            if (userDto is null)
            {
                _logger.LogWarning("User not found during refresh: {UserId}", userId);
                return (HappyCraftStatusCode.RECORD_NOT_FOUND, null);
            }

            var scopes = RoleScopeMap.GetScopesForRole(userDto.Role);

            // Generate new tokens
            var newAccessToken = _jwtTokenService.GenerateAccessToken(userDto, scopes);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var newRefreshTokenHash = HashToken(newRefreshToken);

            // Store new refresh token and revoke old one
            var expiresAt = DateTime.UtcNow.AddDays(14);
            var (revokeStatus, revokeSuccess) = await _authDal.RevokeRefreshTokenAsync(refreshTokenHash, newRefreshTokenHash, ipAddress);
            
            if (!revokeSuccess)
            {
                _logger.LogError("Failed to revoke old refresh token during rotation for user: {UserId}. Aborting rotation.", userId);
                return (revokeStatus, null);
            }

            var (_, storeSuccess) = await _authDal.StoreRefreshTokenAsync(userId.Value, newRefreshTokenHash, expiresAt, ipAddress);

            if (!storeSuccess)
            {
                _logger.LogError("Failed to store new refresh token during rotation for user: {UserId}", userId);
                return (HappyCraftStatusCode.DB_ERROR, null);
            }

            var response = new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = _jwtTokenService.GetAccessTokenExpirySeconds()
            };

            _logger.LogInformation("Token refreshed for user: {UserId}", userId);
            return (HappyCraftStatusCode.OK, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh.");
            return (HappyCraftStatusCode.INTERNAL_ERROR, null);
        }
    }

    public async Task<(int statusCode, bool success)> RevokeTokenAsync(string refreshToken, string? ipAddress = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return (HappyCraftStatusCode.INVALID_REQUEST, false);

            var refreshTokenHash = HashToken(refreshToken);
            var (status, success) = await _authDal.RevokeRefreshTokenAsync(refreshTokenHash, null, ipAddress);

            if (success)
                _logger.LogInformation("Refresh token revoked.");
            else
                _logger.LogWarning("Attempted to revoke non-existent or already revoked token.");

            return (status, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation.");
            return (HappyCraftStatusCode.INTERNAL_ERROR, false);
        }
    }

    public async Task<(int statusCode, VerifyResponseDto? response)> GetCurrentUserAsync(Guid userId)
    {
        try
        {
            var userDto = await _authDal.GetUserByIdAsync(userId);
            if (userDto is null)
                return (HappyCraftStatusCode.RECORD_NOT_FOUND, null);

            var scopes = RoleScopeMap.GetScopesForRole(userDto.Role);

            var response = new VerifyResponseDto
            {
                Id = userDto.Id,
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Role = userDto.Role,
                Status = userDto.Status,
                Scopes = scopes
            };

            return (HappyCraftStatusCode.OK, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching current user details for user: {UserId}", userId);
            return (HappyCraftStatusCode.INTERNAL_ERROR, null);
        }
    }

    private static string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }
}
