using Supabase.Gotrue;

namespace CBOS.Components.Services;

/// <summary>
/// Scoped service that holds the authenticated user's session for the lifetime
/// of the Blazor Server circuit. Register as Scoped in Program.cs.
/// </summary>
public class AuthService
{
    private Session? _session;
    private string   _firstName = "";        // ← ADDED

    public Session? CurrentSession => _session;
    public bool IsAuthenticated => _session?.User != null;
    public string? UserEmail => _session?.User?.Email;
    public string FirstName => _firstName;   // ← ADDED

    public void SetSession(Session session, string firstName = "")  // ← ADDED firstName param
    {
        _session   = session;
        _firstName = firstName;              // ← ADDED
        NotifyStateChanged();
    }

    public void ClearSession()
    {
        _session   = null;
        _firstName = "";                     // ← ADDED
        NotifyStateChanged();
    }

    /// <summary>
    /// Subscribe to this event in components that need to react to auth changes.
    /// </summary>
    public event Action? OnAuthStateChanged;

    private void NotifyStateChanged() => OnAuthStateChanged?.Invoke();
}