using HappyCraftEvent.Contracts.Enums;

namespace HappyCraftEvent.Contracts.DTOs.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserSummaryDto User { get; set; } = null!;
}

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public IEnumerable<string> Scopes { get; set; } = [];
}
