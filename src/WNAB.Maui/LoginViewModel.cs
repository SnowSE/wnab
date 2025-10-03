using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Maui.Services;

namespace WNAB.Maui;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isAuthenticated;

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        _ = CheckAuthenticationStatusAsync();
    }

    private async Task CheckAuthenticationStatusAsync()
    {
        IsAuthenticated = await _authenticationService.IsAuthenticatedAsync();
        if (IsAuthenticated)
        {
            var userName = _authenticationService.GetUserName();
            StatusMessage = $"Logged in as {userName}";
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            StatusMessage = "Logging in...";
            var success = await _authenticationService.LoginAsync();

            if (success)
            {
                IsAuthenticated = true;
                var userName = _authenticationService.GetUserName();
                StatusMessage = $"Successfully logged in as {userName}";

                if (Shell.Current is not null)
                    await Shell.Current.DisplayAlert("Success", "Login successful!", "OK");
            }
            else
            {
                StatusMessage = "Login failed. Please try again.";
                if (Shell.Current is not null)
                    await Shell.Current.DisplayAlert("Error", "Login failed. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Error", $"Login error: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            StatusMessage = "Logging out...";
            await _authenticationService.LogoutAsync();
            IsAuthenticated = false;
            StatusMessage = "Logged out successfully";

            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Success", "Logout successful!", "OK");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlert("Error", $"Logout error: {ex.Message}", "OK");
        }
    }
}
