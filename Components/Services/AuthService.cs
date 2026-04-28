using Supabase.Gotrue;

namespace CBOS.Components.Services;

/// <summary>
/// Scoped service that holds the authenticated user's session for the lifetime
/// of the Blazor Server circuit. Register as Scoped in Program.cs.
/// </summary>
public class AuthService
{
    private Session? _session;

    public Session? CurrentSession => _session;
    public bool IsAuthenticated => _session?.User != null;
    public string? UserEmail => _session?.User?.Email;

    public void SetSession(Session session)
    {
        _session = session;
        NotifyStateChanged();
    }

    public void ClearSession()
    {
        _session = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Subscribe to this event in components that need to react to auth changes.
    /// </summary>
    public event Action? OnAuthStateChanged;

    private void NotifyStateChanged() => OnAuthStateChanged?.Invoke();
}
