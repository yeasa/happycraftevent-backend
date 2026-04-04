namespace HappyCraftEvent.Contracts.StatusCodes;

public record HappyCraftStatusCode
{
    public const int OK                      =      0;
    public const int INTERNAL_ERROR          = -16000;
    public const int DB_ERROR                = -20000;
    public const int RECORD_NOT_FOUND        = -20001;
    public const int RECORD_ALREADY_EXISTS   = -20002;
    public const int INSERTION_FAILED        = -20003;
    public const int UPDATION_FAILED         = -20004;
    public const int DELETION_FAILED         = -20005;
    public const int INVALID_REQUEST         = -20009;
    public const int NOT_ALLOWED             = -20010;
    public const int UNAUTHORIZED            = -20012;
    public const int INVALID_PARAM           = -20013;
    public const int INVALID_ROLE            = -20015;
    public const int FORBIDDEN               = -20017;
    public const int CONFLICT                = -20025;
}
