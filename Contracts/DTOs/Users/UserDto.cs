using HappyCraftEvent.Contracts.Enums;

namespace HappyCraftEvent.Contracts.DTOs.Users;

public class UserDto
{
    public Guid       Id        { get; set; }
    public string     FirstName { get; set; } = string.Empty;
    public string?    LastName  { get; set; }
    public GendersEnum?    Gender    { get; set; }
    public string     Email     { get; set; } = string.Empty;
    public string?    Phone     { get; set; }
    public UserRole   Role      { get; set; }
    public UserStatus Status    { get; set; }
    public DateTime   CreatedAt { get; set; }
    public DateTime   UpdatedAt { get; set; }
}
