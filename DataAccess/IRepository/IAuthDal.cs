using HappyCraftEvent.Contracts.DTOs.Users;

namespace HappyCraftEvent.DataAccess.IRepository;

public interface IAuthDal
{
    /// <summary>
    /// Retrieves user by email if active and not deleted, returns UserDto and password hash.
    /// </summary>
    Task<(int statusCode, UserDto user, string passwordHash)?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Stores a hashed refresh token for a user.
    /// </summary>
    Task<(int statusCode, bool success)> StoreRefreshTokenAsync(Guid userId, string tokenHash, DateTime expiresAt, string? ipAddress);

    /// <summary>
    /// Retrieves and validates a refresh token.
    /// </summary>
    Task<(int statusCode, Guid? userId, bool valid)> ValidateRefreshTokenAsync(string tokenHash);

    /// <summary>
    /// Revokes a refresh token and optionally marks it as replaced.
    /// </summary>
    Task<(int statusCode, bool success)> RevokeRefreshTokenAsync(string tokenHash, string? replacedByTokenHash, string? ipAddress);

    /// <summary>
    /// Gets user details by ID if active and not deleted.
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(Guid userId);
}
