using HappyCraftEvent.Contracts.DTOs.Common;
using HappyCraftEvent.Contracts.DTOs.Users;
using HappyCraftEvent.Contracts.Enums;
using HappyCraftEvent.Contracts.StatusCodes;
using HappyCraftEvent.DataAccess.IRepository;
using HappyCraftEvent.Helper.IService;

namespace HappyCraftEvent.Helper.Service;

public class UserService : IUserService
{
    private readonly IUsersDal _usersDal;
    private readonly ILogger<UserService> _logger;

    public UserService(IUsersDal usersDal, ILogger<UserService> logger)
    {
        _usersDal = usersDal;
        _logger   = logger;
    }

    public async Task<(int statusCode, PaginatedResponseDto<UserDto>? result)> GetUsersAsync(UserListQueryDto query)
    {
        try
        {
            if (query.PageNumber < 1) query.PageNumber = 1;
            if (query.PagePerRow < 1) query.PagePerRow = 10;

            var (statusCode, users, totalCount) = await _usersDal.GetUsersAsync(query);

            if (statusCode != HappyCraftStatusCode.OK)
                return (statusCode, null);

            return (HappyCraftStatusCode.OK, new PaginatedResponseDto<UserDto>
            {
                PageNumber = query.PageNumber,
                PagePerRow = query.PagePerRow,
                TotalCount = totalCount,
                Data       = users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUsersAsync.");
            return (HappyCraftStatusCode.INTERNAL_ERROR, null);
        }
    }

    public async Task<(int statusCode, Guid? userId)> UpsertUserAsync(OperationType operation, UpsertUserRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return (HappyCraftStatusCode.INVALID_REQUEST, null);

            if (operation == OperationType.Add)
            {
                if (string.IsNullOrWhiteSpace(request.Password))
                    return (HappyCraftStatusCode.INVALID_REQUEST, null);

                if (string.IsNullOrWhiteSpace(request.FirstName))
                    return (HappyCraftStatusCode.INVALID_REQUEST, null);

                if (request.Role is null)
                    return (HappyCraftStatusCode.INVALID_REQUEST, null);
            }

            string? passwordHash = null;
            if (operation == OperationType.Add && !string.IsNullOrWhiteSpace(request.Password))
                passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var (statusCode, userId) = await _usersDal.UpsertUserAsync(request, passwordHash);

            return (statusCode, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpsertUserAsync. Operation: {Operation}", operation);
            return (HappyCraftStatusCode.INTERNAL_ERROR, null);
        }
    }
}
