using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;

namespace WNAB.Maui;

// LLM-Dev:v2 Minimal VM to save a user ID to SecureStorage. Updated to use Shell.Current for alerts (avoids deprecated Application.MainPage).
public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    private string? userId;

    [RelayCommand]
    private async Task SaveUserId()
    {
        // Basic validation
        var id = (UserId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Missing User ID", "Please enter a user ID.", "OK");
            return;
        }

        // Persist securely
        await SecureStorage.Default.SetAsync("userId", id);

        if (Shell.Current is not null)
            await Shell.Current.DisplayAlert("Saved", "User ID saved securely.", "OK");
    }
}
