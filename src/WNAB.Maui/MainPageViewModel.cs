using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;

namespace WNAB.Maui;

public partial class MainPageViewModel : ObservableObject
{
    private readonly IPopupService _popupService;

    public MainPageViewModel(IPopupService popupService)
    {
        _popupService = popupService;
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
            var id = await SecureStorage.Default.GetAsync("userId");
            var hasId = !string.IsNullOrWhiteSpace(id);
            UserDisplay = hasId ? $"Signed in as: {id}" : "Not signed in";
            IsSignedIn = hasId; // LLM-Dev:v6 Keep boolean in sync
        }
        catch
        {
            // If SecureStorage is unavailable or throws, degrade gracefully
            UserDisplay = "Not signed in";
            IsSignedIn = false; // LLM-Dev:v6 Ensure consistent state on failure
        }
    }

    // LLM-Dev:v3 Updated to use SecureStorage directly for sign-out
    [RelayCommand]
    private async Task SignOut()
    {
        try
        {
            SecureStorage.Default.Remove("userId");
            UserDisplay = "Not signed in";
            IsSignedIn = false; // LLM-Dev:v6 Explicitly reflect sign-out

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
            IsSignedIn = false; // LLM-Dev:v6 Maintain consistent state
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
    private async Task NavigateToPlanBudget()
    {
        await Shell.Current.GoToAsync("PlanBudget");
    }

    // LLM-Dev:v8 Updated to use popup service since LoginPage is now a popup
    [RelayCommand]
    private async Task NavigateToLogin()
    {
        await _popupService.ShowLoginAsync();
        // LLM-Dev:v8 Refresh user display after login popup closes
        await RefreshUserId();
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
