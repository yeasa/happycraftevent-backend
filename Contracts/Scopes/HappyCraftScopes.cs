namespace HappyCraftEvent.Contracts.Scopes;

/// <summary>
/// Defines all scopes (permissions) available in the HappyCraft system.
/// Scopes are fine-grained permissions used alongside roles for authorization.
/// </summary>
public static class HappyCraftScopes
{
    // Users scopes
    public const string UsersRead  = "users.read";
    public const string UsersWrite = "users.write";

    // Events scopes
    public const string EventsRead        = "events.read";
    public const string EventsWrite       = "events.write";
    public const string EventsAssignAdmin = "events.assign.admin";

    // Guests scopes
    public const string GuestsRead    = "guests.read";
    public const string GuestsWrite   = "guests.write";

    /// <summary>
    /// Returns all scopes available in the system.
    /// </summary>
    public static IEnumerable<string> All => new[]
    {
        UsersRead, UsersWrite,
        EventsRead, EventsWrite, EventsAssignAdmin,
        GuestsRead, GuestsWrite
    };
}
