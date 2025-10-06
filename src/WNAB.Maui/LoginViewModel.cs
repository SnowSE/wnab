using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using WNAB.Logic;
using System;
using System.Threading.Tasks;

namespace WNAB.Maui;

// LLM-Dev:v4 Updated to validate user exists in database before allowing login. Removed NavigateToHome command as popup auto-closes.
public partial class LoginViewModel : ObservableObject
{
    private readonly UserManagementService _userService;

    [ObservableProperty]
    private string? userId;

    public event EventHandler? RequestClose; // Raised to close popup

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    public LoginViewModel(UserManagementService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

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

        // Validate user ID is a number
        if (!int.TryParse(id, out int userIdInt))
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Invalid User ID", "User ID must be a number.", "OK");
            return;
        }

        try
        {
            // LLM-Dev:v3 Check if user exists in database before allowing login
            var user = await _userService.GetUserByIdAsync(userIdInt);
            if (user is null)
            {
                if (Shell.Current is not null)
                    await Shell.Current.DisplayAlert("User Not Found", "The specified user ID does not exist in the database or is inactive.", "OK");
                return;
            }

            // User exists, proceed with login
            await SecureStorage.Default.SetAsync("userId", id);

            if (Shell.Current is not null)
            // purposely not saying await so that we don't have two popups showing at once :) -OA 10/3/2025
                Shell.Current.DisplayAlert("Login Successful", $"Welcome {user.FirstName} {user.LastName}!", "OK");

            // LLM-Dev:v4 Close popup after successful login (MainPageViewModel will refresh display)
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Login Error", $"Failed to validate user: {ex.Message}", "OK");
        }
    }
}
