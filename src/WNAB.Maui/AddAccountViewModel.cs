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
    private int userId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private string statusMessage = "Loading...";

    public AddAccountViewModel(AccountManagementService accounts)
    {
        _accounts = accounts;
    }

    // LLM-Dev: v2 Added initialization to automatically load user ID from secure storage (opposite of login save)
    public async Task InitializeAsync()
    {
        try
        {
            var userIdString = await SecureStorage.Default.GetAsync("userId");
            if (!string.IsNullOrWhiteSpace(userIdString) && int.TryParse(userIdString, out var parsedUserId))
            {
                UserId = parsedUserId;
                IsLoggedIn = true;
                StatusMessage = $"Creating account for user {UserId}";
            }
            else
            {
                IsLoggedIn = false;
                StatusMessage = "Please log in first";
            }
        }
        catch
        {
            IsLoggedIn = false;
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
        if (!IsLoggedIn || UserId <= 0 || string.IsNullOrWhiteSpace(Name))
            return;

        try
        {
            var record = AccountManagementService.CreateAccountRecord(Name);
            await _accounts.CreateAccountAsync(UserId, record);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating account: {ex.Message}";
        }
    }
}