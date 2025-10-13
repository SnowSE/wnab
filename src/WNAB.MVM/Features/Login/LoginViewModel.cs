using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for Login feature - thin coordination layer between View and Model.
/// Handles UI-specific concerns like alerts and popup closing, delegates business logic to Model.
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    public LoginModel Model { get; }

    public event EventHandler? RequestClose; // Raised to close popup

    public LoginViewModel(LoginModel model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Cancel command - closes the login popup without saving.
    /// Pure UI coordination.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Save User ID command - delegates to Model then shows appropriate UI feedback.
    /// Coordinates between Model's business logic and View's presentation.
    /// </summary>
    [RelayCommand]
    private async Task SaveUserId()
    {
        // Delegate to Model for business logic
        var result = await Model.PerformLoginAsync();

        // Handle UI presentation based on result
        if (!result.Success)
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlertAsync("Login Failed", result.Message, "OK");
            return;
        }

        // Success - show welcome message and close popup
        if (Shell.Current is not null)
        {
            // Purposely not awaiting so that we don't have two popups showing at once :) -OA 10/3/2025
            // Well I don't like the errors and warnings, so I'm awaiting it :( - KB 10/13/2025
            await Shell.Current.DisplayAlertAsync("Login Successful", result.Message, "OK");
        }

        // Close popup after successful login (MainPageViewModel will refresh display)
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
