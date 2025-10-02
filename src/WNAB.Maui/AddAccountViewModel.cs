using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic;
using Microsoft.Maui.Storage;

namespace WNAB.Maui;

public partial class AddAccountViewModel : ObservableObject
{
    private readonly AccountManagementService _accounts;

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready to create account";

    // LLM-Dev:v3 Removed userId, isLoggedIn properties - internal only, user doesn't need to see
    private int _userId;
    private bool _isLoggedIn;

    public AddAccountViewModel(AccountManagementService accounts)
    {
        _accounts = accounts;
    }

    // LLM-Dev:v3 Simplified initialization - only load user ID internally, no UI updates about user ID
    public async Task InitializeAsync()
    {
        try
        {
            var userIdString = await SecureStorage.Default.GetAsync("userId");
            if (!string.IsNullOrWhiteSpace(userIdString) && int.TryParse(userIdString, out var parsedUserId))
            {
                _userId = parsedUserId;
                _isLoggedIn = true;
                StatusMessage = "Ready to create account";
            }
            else
            {
                _isLoggedIn = false;
                StatusMessage = "Please log in first";
            }
        }
        catch
        {
            _isLoggedIn = false;
            StatusMessage = "Error checking login status";
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (!_isLoggedIn || _userId <= 0)
        {
            StatusMessage = "Please log in first to create an account";
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Please enter an account name";
            return;
        }

        try
        {
            StatusMessage = "Creating account...";
            var record = AccountManagementService.CreateAccountRecord(Name);
            await _accounts.CreateAccountAsync(_userId, record);
            StatusMessage = "Account created successfully!";

            // LLM-Dev:v3 Clear the name field for next use
            Name = string.Empty;

            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating account: {ex.Message}";
        }
    }
}