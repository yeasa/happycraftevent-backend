using HappyCraftEvent.Contracts.DTOs.Common;
using HappyCraftEvent.Contracts.DTOs.Users;
using HappyCraftEvent.Contracts.Enums;

namespace HappyCraftEvent.Helper.IService;

public interface IUserService
{
    Task<(int statusCode, PaginatedResponseDto<UserDto>? result)> GetUsersAsync(UserListQueryDto query);
    Task<(int statusCode, Guid? userId)> UpsertUserAsync(OperationType operation, UpsertUserRequestDto request);
}
