using HappyCraftEvent.Contracts.Enums;

namespace HappyCraftEvent.Contracts.DTOs.Users;

public class UpsertUserRequestDto
{
    public string?     FirstName { get; set; }
    public string?     LastName  { get; set; }
    public GendersEnum?     Gender  { get; set; }
    public string?     Email     { get; set; }
    public string?     Phone     { get; set; }
    public string?     Password  { get; set; }
    public UserRole?   Role      { get; set; }
    public UserStatus? Status    { get; set; }
}
