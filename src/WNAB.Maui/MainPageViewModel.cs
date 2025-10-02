using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Views;
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

    [RelayCommand]
    private async Task OpenNewTransaction()
    {
        await _popupService.ShowNewTransactionAsync();
    }

    [RelayCommand]
    private async Task OpenAddCategory()
    {
        await _popupService.ShowAddCategoryAsync();
    }

    [RelayCommand]
    private async Task OpenAddUser()
    {
        await _popupService.ShowAddUserAsync();
    }

    [RelayCommand]
    private async Task OpenAddAccount()
    {
        await _popupService.ShowAddAccountAsync();
    }
}
