using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using WNAB.Services;

namespace WNAB.MVM;

public partial class AddAccountViewModel : ObservableObject
{
    private readonly AccountManagementService _accounts;
    private readonly IAuthenticationService _authService;

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready to create account";

    // LLM-Dev:v3 Track login status internally
    private bool _isLoggedIn;

    public AddAccountViewModel(AccountManagementService accounts, IAuthenticationService authService)
    {
        _accounts = accounts;
        _authService = authService;
    }

    // LLM-Dev:v3 Simplified initialization - check login status
    public async Task InitializeAsync()
    {
        try
        {
            _isLoggedIn = await _authService.IsAuthenticatedAsync();
            StatusMessage = _isLoggedIn ? "Ready to create account" : "Please log in first";
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
        if (!_isLoggedIn)
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
            // API derives user from token; pass 0 for UserId
            var record = new AccountRecord(Name);
            await _accounts.CreateAccountAsync(record);
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