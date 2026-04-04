namespace HappyCraftEvent.Contracts.DTOs.Users;

public class UserListQueryDto
{
    public int     PageNumber { get; set; } = 1;
    public int     PagePerRow { get; set; } = 10;
    public string? FirstName  { get; set; }
    public string? LastName   { get; set; }
    public string? Email      { get; set; }
    public string? Phone      { get; set; }
}
