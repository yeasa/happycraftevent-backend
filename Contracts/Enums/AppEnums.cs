namespace HappyCraftEvent.Contracts.Enums;

public enum OperationType
{
    Add  = 0,
    Edit = 1
}

public enum UserRole
{
    ADMIN,
    EVENT_ADMIN
}

public enum UserStatus
{
    Active,
    Disabled,
    Uninitialized
}
