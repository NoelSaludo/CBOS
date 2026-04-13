using System.Security.Claims;
using CBOS.Components.Pages.Admin;
using Microsoft.AspNetCore.Components.Authorization;

class AdminAuthenticator : AuthenticationStateProvider
{
    private AdminSupabase _supabase;
    private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
    public AdminAuthenticator(AdminSupabase supabase)
    {
        _supabase = supabase;
    }
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public async Task Authenticate(string id)
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, "CBOSAuth");
        var user = new ClaimsPrincipal(identity);

        _currentUser = user;

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void Logout()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public ClaimsPrincipal GetCurrentUser()
    {
        return _currentUser;
    }
}