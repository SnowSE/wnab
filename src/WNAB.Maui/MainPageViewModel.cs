using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.Logging;
using WNAB.Maui.Services;

namespace WNAB.Maui;

public partial class MainPageViewModel : ObservableObject
{
    private readonly IPopupService _popupService;
    private readonly IAuthenticationService _authenticationService;

    public MainPageViewModel(IPopupService popupService, IAuthenticationService authenticationService, ILogger<MainPageViewModel> logger)
    {
        _popupService = popupService;
        _authenticationService = authenticationService;
        // LLM-Dev:v6 Initialize sign-in state on construction (fire and forget)
        _ = RefreshUserId();
    }

    // LLM-Dev:v3 Updated to use SecureStorage directly like LoginViewModel
    [ObservableProperty]
    private string userDisplay = "Not signed in";

    [ObservableProperty]
    private bool isSignedIn;

    // LLM-Dev:v7 Added dynamic auth toolbar text (dependent on IsSignedIn)
    public string LogInSignOutText => IsSignedIn ? "Sign Out" : "Log In";


    // LLM-Dev:v7 Notify toolbar text when sign-in state changes
    partial void OnIsSignedInChanged(bool value)
    {
        OnPropertyChanged(nameof(LogInSignOutText));
    }

    [RelayCommand]
    private async Task RefreshUserId()
    {
        try
        {
            var isAuthenticated = await _authenticationService.IsAuthenticatedAsync();
            if (isAuthenticated)
            {
                var userName = _authenticationService.GetUserName();
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

    [RelayCommand]
    private async Task SignOut()
    {
        try
        {
            await _authenticationService.LogoutAsync();
            SecureStorage.Default.Remove("userId");
            UserDisplay = "Not signed in";
            IsSignedIn = false;

            if (Shell.Current is not null)
            {
                // Optional confirmation toast/alert
                await Shell.Current.DisplayAlert("Signed out", "You have been signed out.", "OK");
            }
        }
        catch
        {
            // Swallow any storage-related errors; UI stays in a safe 'not signed in' state
            UserDisplay = "Not signed in";
            IsSignedIn = false;
        }
    }

    // LLM-Dev:v5 Updated navigation commands to use new route-based navigation
    [RelayCommand]
    private async Task NavigateToCategories()
    {
        await Shell.Current.GoToAsync("Categories");
    }

    [RelayCommand]
    private async Task NavigateToAccounts()
    {
        await Shell.Current.GoToAsync("Accounts");
    }

    [RelayCommand]
    private async Task NavigateToTransactions()
    {
        await Shell.Current.GoToAsync("Transactions");
    }

    [RelayCommand]
    private async Task NavigateToUsers()
    {
        await Shell.Current.GoToAsync("Users");
    }

    [RelayCommand]
    private async Task NavigateToLogin()
    {
        var success = await _authenticationService.LoginAsync();
        if (success)
        {
            // Refresh user display after successful login
            await RefreshUserId();
        }
        else
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.DisplayAlert("Login Failed", "Unable to authenticate. Please try again.", "OK");
            }
        }
    }

    // LLM-Dev:v7 Added unified auth toolbar command routing to sign-in / sign-out logic
    [RelayCommand]
    private async Task LogInSignOut()
    {
        if (IsSignedIn)
        {
            await SignOut();
        }
        else
        {
            await NavigateToLogin();
        }
    }
}
