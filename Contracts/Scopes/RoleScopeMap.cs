using HappyCraftEvent.Contracts.Enums;

namespace HappyCraftEvent.Contracts.Scopes;

/// <summary>
/// Maps user roles to their allowed scopes (permissions).
/// This is the central configuration determining what each role can do.
/// </summary>
public static class RoleScopeMap
{
    /// <summary>
    /// Dictionary mapping UserRole to the collection of scopes allowed for that role.
    /// </summary>
    public static readonly IReadOnlyDictionary<UserRole, IReadOnlyCollection<string>> Map =
        new Dictionary<UserRole, IReadOnlyCollection<string>>
        {
            [UserRole.ADMIN] = new[]
            {
                HappyCraftScopes.UsersRead,
                HappyCraftScopes.UsersWrite,
                HappyCraftScopes.EventsRead,
                HappyCraftScopes.EventsWrite,
                HappyCraftScopes.EventsAssignAdmin,
                HappyCraftScopes.GuestsRead,
                HappyCraftScopes.GuestsWrite
            },
            [UserRole.EVENT_ADMIN] = new[]
            {
                HappyCraftScopes.EventsRead,
                HappyCraftScopes.EventsWrite,
                HappyCraftScopes.GuestsRead,
                HappyCraftScopes.GuestsWrite
            }
        };

    /// <summary>
    /// Returns the scopes for a given role.
    /// </summary>
    public static IReadOnlyCollection<string> GetScopesForRole(UserRole role)
    {
        return Map.TryGetValue(role, out var scopes)
            ? scopes
            : Array.Empty<string>();
    }
}
