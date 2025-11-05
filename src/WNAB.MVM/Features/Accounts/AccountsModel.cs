using System.Collections.ObjectModel;
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

            var list = await _accounts.GetAccountsForUserAsync();
            foreach (var account in list)
                Items.Add(new AccountItemViewModel(account));

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

    /// <summary>
    /// Update an account with new name and type.
    /// </summary>
    public async Task<bool> UpdateAccountAsync(int accountId, string newName, AccountType newAccountType)
    {
        try
        {
            var success = await _accounts.UpdateAccountAsync(accountId, newName, newAccountType);
            return success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error updating account: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Delete an account by ID.
    /// </summary>
    public async Task<bool> DeleteAccountAsync(int accountId)
    {
        try
        {
            var success = await _accounts.DeleteAccountAsync(accountId);
            if (success)
            {
                // Remove from local collection
                var item = Items.FirstOrDefault(i => i.Id == accountId);
                if (item != null)
                {
                    Items.Remove(item);
                }
                StatusMessage = $"Account deleted successfully";
            }
            return success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting account: {ex.Message}";
            return false;
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
            if (ShowInactive)
            {
                await LoadInactiveAccountsAsync();
            }
            else
            {
                await LoadAccountsAsync();
            }
        }
    }

    /// <summary>
    /// Load inactive accounts for the current authenticated user.
    /// </summary>
    public async Task LoadInactiveAccountsAsync()
    {
        if (IsBusy || !IsLoggedIn) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Loading inactive accounts...";
            InactiveItems.Clear();

            var list = await _accounts.GetInactiveAccountsAsync();
            foreach (var account in list)
                InactiveItems.Add(new AccountItemViewModel(account));

            StatusMessage = list.Count == 0 ? "No inactive accounts found" : $"Loaded {list.Count} inactive accounts";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading inactive accounts: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Reactivate an inactive account by ID.
    /// </summary>
    public async Task<bool> ReactivateAccountAsync(int accountId)
    {
        try
        {
            var success = await _accounts.ReactivateAccountAsync(accountId);
            if (success)
            {
                // Remove from inactive collection
                var item = InactiveItems.FirstOrDefault(i => i.Id == accountId);
                if (item != null)
                {
                    InactiveItems.Remove(item);
                }
                StatusMessage = $"Account reactivated successfully";
            }
            return success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error reactivating account: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Toggle between showing active and inactive accounts.
    /// </summary>
    public async Task ToggleShowInactiveAsync()
    {
        ShowInactive = !ShowInactive;
        
        if (ShowInactive)
        {
            await LoadInactiveAccountsAsync();
        }
        else
        {
            await LoadAccountsAsync();
        }
    }
}
