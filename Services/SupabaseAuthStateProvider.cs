using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MechanicalCalculatorWeb.Services;

/// <summary>
/// Bridges <see cref="SupabaseAuthService"/> to Blazor's authorization system, so
/// &lt;AuthorizeView&gt; and [Authorize] see the current user. Holds no state of its
/// own — the session lives in the auth service; this just projects it as a
/// ClaimsPrincipal and relays change notifications.
/// </summary>
public class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly SupabaseAuthService _auth;

    public SupabaseAuthStateProvider(SupabaseAuthService auth)
    {
        _auth = auth;
        _auth.AuthStateChanged += () =>
            NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(BuildState());

    private AuthenticationState BuildState()
    {
        var session = _auth.CurrentSession;
        if (session == null || session.IsExpired)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.UserId),
            new Claim(ClaimTypes.Email, session.Email),
            new Claim(ClaimTypes.Name, session.Email)
        }, authenticationType: "supabase");

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
