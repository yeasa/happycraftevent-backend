using Dapper;
using HappyCraftEvent.Contracts.DTOs.Users;
using HappyCraftEvent.Contracts.Enums;
using HappyCraftEvent.Contracts.StatusCodes;
using HappyCraftEvent.DataAccess.IRepository;
using Npgsql;
using System.Security.Cryptography;
using System.Text;

namespace HappyCraftEvent.DataAccess.Repository;

public class AuthDal : IAuthDal
{
    private readonly string _connectionString;
    private readonly ILogger<AuthDal> _logger;

    public AuthDal(IConfiguration configuration, ILogger<AuthDal> logger)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
        _logger = logger;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }

    public async Task<(int statusCode, UserDto user, string passwordHash)?> GetUserByEmailAsync(string email)
    {
        try
        {
            const string sql = """
                SELECT id, email, first_name, last_name, password_hash, role, status, gender, created_at, updated_at
                FROM users
                WHERE LOWER(email) = LOWER(@Email)
                  AND is_deleted = FALSE
                LIMIT 1
                """;

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { Email = email });

            if (result is null)
                return null;

            var userDto = new UserDto
            {
                Id = (Guid)result.id,
                Email = (string)result.email,
                FirstName = (string)result.first_name,
                LastName = (string?)result.last_name,
                Gender = result.gender is not null ? Enum.Parse<GendersEnum>((string)result.gender) : null,
                Role = Enum.Parse<UserRole>((string)result.role),
                Status = Enum.Parse<UserStatus>((string)result.status),
                CreatedAt = (DateTime)result.created_at,
                UpdatedAt = (DateTime)result.updated_at
            };

            return (HappyCraftStatusCode.OK, userDto, (string)result.password_hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            return null;
        }
    }

    public async Task<(int statusCode, bool success)> StoreRefreshTokenAsync(Guid userId, string tokenHash, DateTime expiresAt, string? ipAddress)
    {
        try
        {
            const string sql = """
                INSERT INTO refresh_tokens (user_id, token_hash, expires_at, created_by_ip)
                VALUES (@UserId, @TokenHash, @ExpiresAt, @IpAddress)
                """;

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var rowsAffected = await conn.ExecuteAsync(sql, new
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                IpAddress = ipAddress
            });

            return (HappyCraftStatusCode.OK, rowsAffected > 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing refresh token for user: {UserId}", userId);
            return (HappyCraftStatusCode.DB_ERROR, false);
        }
    }

    public async Task<(int statusCode, Guid? userId, bool valid)> ValidateRefreshTokenAsync(string tokenHash)
    {
        try
        {
            const string sql = """
                SELECT user_id, expires_at, revoked_at, replaced_by_token_hash
                FROM refresh_tokens
                WHERE token_hash = @TokenHash
                LIMIT 1
                """;

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var token = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { TokenHash = tokenHash });

            if (token is null)
            {
                _logger.LogWarning("Refresh token not found.");
                return (HappyCraftStatusCode.RECORD_NOT_FOUND, null, false);
            }

            if (token.revoked_at is not null)
            {
                _logger.LogWarning("Refresh token is revoked.");
                return (HappyCraftStatusCode.UNAUTHORIZED, null, false);
            }

            if (token.replaced_by_token_hash is not null)
            {
                _logger.LogWarning("Refresh token has been replaced (rotated).");
                return (HappyCraftStatusCode.UNAUTHORIZED, null, false);
            }

            if ((DateTime)token.expires_at < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token is expired.");
                return (HappyCraftStatusCode.UNAUTHORIZED, null, false);
            }

            return (HappyCraftStatusCode.OK, (Guid)token.user_id, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token.");
            return (HappyCraftStatusCode.DB_ERROR, null, false);
        }
    }

    public async Task<(int statusCode, bool success)> RevokeRefreshTokenAsync(string tokenHash, string? replacedByTokenHash, string? ipAddress)
    {
        try
        {
            const string sql = """
                UPDATE refresh_tokens
                SET revoked_at = NOW(),
                    replaced_by_token_hash = @ReplacedByTokenHash,
                    revoked_by_ip = @IpAddress
                WHERE token_hash = @TokenHash
                """;

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var rowsAffected = await conn.ExecuteAsync(sql, new
            {
                TokenHash = tokenHash,
                ReplacedByTokenHash = replacedByTokenHash,
                IpAddress = ipAddress
            });

            return (HappyCraftStatusCode.OK, rowsAffected > 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token.");
            return (HappyCraftStatusCode.DB_ERROR, false);
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            const string sql = """
                SELECT id, email, first_name, last_name, role, status, gender, created_at, updated_at
                FROM users
                WHERE id = @UserId
                  AND is_deleted = FALSE
                LIMIT 1
                """;

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { UserId = userId });

            if (result is null)
                return null;

            return new UserDto
            {
                Id = (Guid)result.id,
                Email = (string)result.email,
                FirstName = (string)result.first_name,
                LastName = (string?)result.last_name,
                Gender = result.gender is not null ? Enum.Parse<GendersEnum>((string)result.gender) : null,
                Role = Enum.Parse<UserRole>((string)result.role),
                Status = Enum.Parse<UserStatus>((string)result.status),
                CreatedAt = (DateTime)result.created_at,
                UpdatedAt = (DateTime)result.updated_at
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
            return null;
        }
    }
}
