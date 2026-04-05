using HappyCraftEvent.Contracts.Enums;

namespace HappyCraftEvent.Contracts.DTOs.Auth;

public class VerifyResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public IEnumerable<string> Scopes { get; set; } = [];
}
