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
    }

    // LLM-Dev:v3 Updated to use SecureStorage directly like LoginViewModel
    [ObservableProperty]
    private string userDisplay = "Not signed in";

    [RelayCommand]
    private async Task RefreshUserId()
    {
        try
        {
            var id = await SecureStorage.Default.GetAsync("userId");
            UserDisplay = string.IsNullOrWhiteSpace(id) ? "Not signed in" : $"Signed in as: {id}";
        }
        catch
        {
            // If SecureStorage is unavailable or throws, degrade gracefully
            UserDisplay = "Not signed in";
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
        await Shell.Current.GoToAsync("Login");
    }

}
