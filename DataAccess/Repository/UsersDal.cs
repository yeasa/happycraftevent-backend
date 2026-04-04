using Dapper;
using HappyCraftEvent.Contracts.DTOs.Users;
using HappyCraftEvent.Contracts.Enums;
using HappyCraftEvent.Contracts.StatusCodes;
using HappyCraftEvent.DataAccess.IRepository;
using Npgsql;

namespace HappyCraftEvent.DataAccess.Repository;

public class UsersDal : IUsersDal
{
    private readonly string _connectionString;
    private readonly ILogger<UsersDal> _logger;

    public UsersDal(IConfiguration configuration, ILogger<UsersDal> logger)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
        _logger = logger;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<(int statusCode, IEnumerable<UserDto> users, int totalCount)> GetUsersAsync(UserListQueryDto query)
    {
        try
        {
            var searchConditions = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(query.FirstName))
            {
                searchConditions.Add("first_name ILIKE @FirstName");
                parameters.Add("FirstName", $"%{query.FirstName.Trim()}%");
            }
            if (!string.IsNullOrWhiteSpace(query.LastName))
            {
                searchConditions.Add("last_name ILIKE @LastName");
                parameters.Add("LastName", $"%{query.LastName.Trim()}%");
            }
            if (!string.IsNullOrWhiteSpace(query.Email))
            {
                searchConditions.Add("email ILIKE @Email");
                parameters.Add("Email", $"%{query.Email.Trim()}%");
            }
            if (!string.IsNullOrWhiteSpace(query.Phone))
            {
                searchConditions.Add("phone ILIKE @Phone");
                parameters.Add("Phone", $"%{query.Phone.Trim()}%");
            }

            var searchWhere = searchConditions.Count > 0
                ? $" AND ({string.Join(" OR ", searchConditions)})"
                : string.Empty;

            parameters.Add("PagePerRow", query.PagePerRow);
            parameters.Add("Offset", (query.PageNumber - 1) * query.PagePerRow);

            var countSql = $"""
                SELECT COUNT(*)
                FROM users
                WHERE is_deleted = FALSE{searchWhere}
                """;

            var selectSql = $"""
                SELECT
                    id          AS "Id",
                    first_name  AS "FirstName",
                    last_name   AS "LastName",
                    email       AS "Email",
                    phone       AS "Phone",
                    role        AS "Role",
                    status      AS "Status",
                    created_at  AS "CreatedAt",
                    updated_at  AS "UpdatedAt"
                FROM users
                WHERE is_deleted = FALSE{searchWhere}
                ORDER BY created_at DESC
                LIMIT  @PagePerRow
                OFFSET @Offset
                """;

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var totalCount = await conn.ExecuteScalarAsync<int>(countSql, parameters);
            var users      = await conn.QueryAsync<UserDto>(selectSql, parameters);

            return (HappyCraftStatusCode.OK, users, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from database.");
            return (HappyCraftStatusCode.DB_ERROR, [], 0);
        }
    }

    public async Task<(int statusCode, Guid? userId)> UpsertUserAsync(
        UpsertUserRequestDto request, string? passwordHash)
    {
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();

            const string upsertSql = """
                INSERT INTO users
                    (email, password_hash, first_name, last_name, phone, role, status, created_at, updated_at)
                VALUES
                    (@Email, @PasswordHash, @FirstName, @LastName, @Phone, @Role, @Status, NOW(), NOW())
                ON CONFLICT (LOWER(email)) WHERE is_deleted = FALSE
                DO UPDATE SET
                    first_name = COALESCE(@FirstName, users.first_name),
                    last_name  = COALESCE(@LastName,  users.last_name),
                    phone      = COALESCE(@Phone,     users.phone),
                    role       = COALESCE(@Role,      users.role),
                    status     = COALESCE(@Status,    users.status)
                RETURNING id;
                """;

            var upsertedId = await conn.ExecuteScalarAsync<Guid?>(upsertSql, new
            {
                request.Email,
                PasswordHash = passwordHash,
                request.FirstName,
                request.LastName,
                request.Phone,
                Role   = request.Role?.ToString(),
                Status = request.Status?.ToString()
            });

            if (upsertedId is null)
                return (HappyCraftStatusCode.DB_ERROR, null);

            return (HappyCraftStatusCode.OK, upsertedId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting user. Email: {Email}", request.Email);
            return (HappyCraftStatusCode.DB_ERROR, null);
        }
    }
}
