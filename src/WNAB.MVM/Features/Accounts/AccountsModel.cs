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

    public ObservableCollection<Account> Items { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private string statusMessage = "Loading...";

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
                Items.Add(account);

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
    /// Refresh accounts by checking session and reloading data.
    /// </summary>
    public async Task RefreshAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadAccountsAsync();
        }
    }
}
