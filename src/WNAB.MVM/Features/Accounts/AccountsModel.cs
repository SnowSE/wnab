using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for Accounts feature.
/// Handles data fetching, authentication state, and account list management.
/// </summary>
public partial class AccountsModel : ObservableObject
{
    private readonly AccountManagementService _accounts;
    private readonly IAuthenticationService _authService;

    public ObservableCollection<AccountItemViewModel> Items { get; } = new();
    public ObservableCollection<AccountItemViewModel> InactiveItems { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private string statusMessage = "Loading...";

    [ObservableProperty]
    private bool showInactive = false;

    public AccountsModel(AccountManagementService accounts, IAuthenticationService authService)
    {
        _accounts = accounts;
        _authService = authService;
    }

    /// <summary>
    /// Initialize the model by checking user session and loading accounts if authenticated.
    /// </summary>
    public async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadAccountsAsync();
        }
    }

    /// <summary>
    /// Check if user is logged in and update authentication state.
    /// </summary>
    public async Task CheckUserSessionAsync()
    {
        try
        {
            IsLoggedIn = await _authService.IsAuthenticatedAsync();
            if (IsLoggedIn)
            {
                var userName = await _authService.GetUserNameAsync();
                StatusMessage = $"Logged in as {userName ?? "user"}";
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

    /// <summary>
    /// Load accounts for the current authenticated user.
    /// </summary>
    public async Task LoadAccountsAsync()
    {
        if (IsBusy || !IsLoggedIn) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Loading accounts...";
            Items.Clear();
            InactiveItems.Clear();

            var list = await _accounts.GetAccountsForUserAsync();
            foreach (var account in list)
            {
                var accountItem = new AccountItemViewModel(account);
                if (account.IsActive)
                    Items.Add(accountItem);
                else
                    InactiveItems.Add(accountItem);
            }

            StatusMessage = list.Count == 0 ? "No accounts found" : $"Loaded {Items.Count} active and {InactiveItems.Count} inactive accounts";
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

    /// <summary>
    /// Update an account with new name, type, and active status.
    /// Returns a tuple with success status and error message (if any).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UpdateAccountAsync(int accountId, string newName, AccountType newAccountType, bool isActive)
    {
        try
        {
            var (success, errorMessage) = await _accounts.UpdateAccountAsync(accountId, newName, newAccountType, isActive);
            if (success)
            {
                StatusMessage = $"Account updated successfully";
                return (true, null);
            }

            StatusMessage = $"Failed to update account: {errorMessage}";
            return (false, errorMessage);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error updating account: {ex.Message}";
            StatusMessage = errorMsg;
            return (false, errorMsg);
        }
    }

    /// <summary>
    /// Delete an account by ID.
    /// Returns a tuple with success status and error message (if any).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> DeactivateAccountAsync(int accountId)
    {
        try
        {
            var (success, errorMessage) = await _accounts.DeactivateAccountAsync(accountId);
            if (success)
            {
                // Remove from local collection
                var item = Items.FirstOrDefault(i => i.Id == accountId);
                if (item != null)
                {
                    Items.Remove(item);
                }
                StatusMessage = $"Account deactivated successfully";
                return (true, null);
            }

            StatusMessage = $"Failed to deactivate account: {errorMessage}";
            return (false, errorMessage);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error deactivating account: {ex.Message}";
            StatusMessage = errorMsg;
            return (false, errorMsg);
        }
    }

    /// <summary>
    /// Refresh accounts by checking session and reloading data.
    /// </summary>
    public async Task RefreshAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            // One load fills both active and inactive collections
            await LoadAccountsAsync();
        }
    }

    /// <summary>
    /// Toggle between showing active and inactive accounts.
    /// </summary>
    public async Task ToggleShowInactiveAsync()
    {
        ShowInactive = !ShowInactive;
        // Reload to ensure collections are up to date
        await LoadAccountsAsync();
    }

    /// <summary>
    /// Reactivate an inactive account and move it to active list
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> ReactivateAccountAsync(int accountId)
    {
        var inactive = InactiveItems.FirstOrDefault(i => i.Id == accountId);
        if (inactive == null)
        {
            return (false, "Account not found in inactive list");
        }
        var (success, errorMessage) = await UpdateAccountAsync(accountId, inactive.AccountName, inactive.AccountType, true);
        if (success)
        {
            InactiveItems.Remove(inactive);
            // Underlying Account model will have been updated by API call; reflect active status
            Items.Add(inactive);
            // Notify consumers if they rely on ShowInactive state
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(InactiveItems));
        }
        return (success, errorMessage);
    }
}
