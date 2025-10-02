using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic;
using WNAB.Logic.Data;
using Microsoft.Maui.Storage;

namespace WNAB.Maui;

// LLM-Dev:v2 Added AddAccount command and IPopupService injection
public partial class AccountsViewModel : ObservableObject
{
    private readonly AccountManagementService _accounts;
    private readonly IPopupService _popupService;

    public ObservableCollection<AccountItem> Items { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int userId;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private string statusMessage = "Loading...";

    public AccountsViewModel(AccountManagementService accounts, IPopupService popupService)
    {
        _accounts = accounts;
        _popupService = popupService;
    }

    // LLM-Dev: v1 Added initialization method to automatically load user session and accounts
    public async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadAccountsAsync();
        }
    }

    // LLM-Dev: v1 Check if user is logged in and get user ID from secure storage (opposite of login save)
    [RelayCommand]
    private async Task CheckUserSessionAsync()
    {
        try
        {
            var userIdString = await SecureStorage.Default.GetAsync("userId");
            if (!string.IsNullOrWhiteSpace(userIdString) && int.TryParse(userIdString, out var parsedUserId))
            {
                UserId = parsedUserId;
                IsLoggedIn = true;
                StatusMessage = $"Logged in as user {UserId}";
            }
            else
            {
                IsLoggedIn = false;
                StatusMessage = "Please log in to view accounts";
                Items.Clear();
            }
        }
        catch
        {
            IsLoggedIn = false;
            StatusMessage = "Error checking login status";
            Items.Clear();
        }
    }

    // LLM-Dev: v1 Renamed from LoadAsync and updated to use stored user ID
    [RelayCommand]
    private async Task LoadAccountsAsync()
    {
        if (IsBusy || !IsLoggedIn || UserId <= 0) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Loading accounts...";
            Items.Clear();
            
            var list = await _accounts.GetAccountsForUserAsync(UserId);
            foreach (var a in list)
                Items.Add(new AccountItem(a.Id, a.AccountName, a.AccountType, a.CachedBalance));
                
            StatusMessage = list.Count == 0 ? "No accounts found" : $"Loaded {list.Count} accounts";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading accounts: {ex.Message}";
        }
        finally 
        { 
            IsBusy = false; 
        }
    }

    // LLM-Dev: v1 Add refresh command for manual reload
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadAccountsAsync();
        }
    }

    // LLM-Dev:v2 Add command to open Add Account popup
    [RelayCommand]
    private async Task AddAccount()
    {
        await _popupService.ShowAddAccountAsync();
        // Refresh the list after popup closes
        await RefreshAsync();
    }
}

public sealed record AccountItem(int Id, string Name, string Type, decimal Balance);