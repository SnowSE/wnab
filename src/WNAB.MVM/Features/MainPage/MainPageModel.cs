using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Storage;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for MainPage feature.
/// Handles authentication state checking, sign-out logic, and user display management.
/// </summary>
public partial class MainPageModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;

    [ObservableProperty]
    private string userDisplay = "Not signed in";

    [ObservableProperty]
    private bool isSignedIn;

    public MainPageModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Get the appropriate text for the login/logout button based on authentication state.
    /// </summary>
    public string GetAuthButtonText() => IsSignedIn ? "Sign Out" : "Log In";

    /// <summary>
    /// Check current authentication status and update user display accordingly.
    /// </summary>
    public async Task CheckAuthenticationAsync()
    {
        try
        {
            var isAuthenticated = await _authenticationService.IsAuthenticatedAsync();
            if (isAuthenticated)
            {
                var userName = await _authenticationService.GetUserNameAsync();
                UserDisplay = !string.IsNullOrWhiteSpace(userName) ? $"Signed in as: {userName}" : "Signed in";
                IsSignedIn = true;
            }
            else
            {
                UserDisplay = "Not signed in";
                IsSignedIn = false;
            }
        }
        catch
        {
            // If authentication check fails, degrade gracefully
            UserDisplay = "Not signed in";
            IsSignedIn = false;
        }
    }

    /// <summary>
    /// Sign out the current user and clear authentication state.
    /// </summary>
    public async Task SignOutAsync()
    {
        try
        {
            await _authenticationService.LogoutAsync();
            SecureStorage.Default.Remove("userId");
            UserDisplay = "Not signed in";
            IsSignedIn = false;
        }
        catch
        {
            // Swallow any storage-related errors; UI stays in a safe 'not signed in' state
            UserDisplay = "Not signed in";
            IsSignedIn = false;
        }
    }
}
