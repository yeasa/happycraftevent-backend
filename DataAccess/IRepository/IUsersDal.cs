using HappyCraftEvent.Contracts.DTOs.Users;

namespace HappyCraftEvent.DataAccess.IRepository;

public interface IUsersDal
{
    Task<(int statusCode, IEnumerable<UserDto> users, int totalCount)> GetUsersAsync(UserListQueryDto query);
    Task<(int statusCode, Guid? userId)> UpsertUserAsync(UpsertUserRequestDto request, string? passwordHash);
}
