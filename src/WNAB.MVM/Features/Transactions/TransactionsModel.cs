using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for Transactions feature.
/// Handles data fetching, authentication state, and transaction list management.
/// </summary>
public partial class TransactionsModel : ObservableObject
{
    private readonly TransactionManagementService _transactions;
    private readonly IAuthenticationService _authService;

    public ObservableCollection<TransactionItem> Items { get; } = new();

    // Holds transaction splits loaded separately from transactions
    public ObservableCollection<TransactionSplitResponse> Splits { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private string statusMessage = "Loading...";

    public TransactionsModel(TransactionManagementService transactions, IAuthenticationService authService)
    {
        _transactions = transactions;
        _authService = authService;
    }

    /// <summary>
    /// Initialize the model by checking user session and loading transactions if authenticated.
    /// </summary>
    public async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadTransactionsAsync();
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
                StatusMessage = "Please log in to view transactions";
                Items.Clear();
                Splits.Clear();
            }
        }
        catch
        {
            IsLoggedIn = false;
            StatusMessage = "Error checking login status";
            Items.Clear();
            Splits.Clear();
        }
    }

    /// <summary>
    /// Load transactions for the current authenticated user (using API response records).
    /// </summary>
    public async Task LoadTransactionsAsync()
    {
        if (IsBusy || !IsLoggedIn) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Loading transactions...";
            Items.Clear();

            var list = await _transactions.GetTransactionsForUserAsync();
            foreach (var t in list)
            {
                // TransactionResponse does not include splits; show placeholder for categories
                var categoriesText = "N/A";

                Items.Add(new TransactionItem(
                    t.Id,
                    t.TransactionDate,
                    t.Payee,
                    t.Description,
                    t.Amount,
                    t.AccountName,
                    categoriesText));
            }

            StatusMessage = list.Count == 0 ? "No transactions found" : $"Loaded {list.Count} transactions";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading transactions: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Load all transaction splits for the current authenticated user.
    /// Uses the new /transactionsplits endpoint without filters.
    /// </summary>
    public async Task LoadAllTransactionSplitsAsync()
    {
        if (!IsLoggedIn) return;

        try
        {
            StatusMessage = "Loading transaction splits...";
            Splits.Clear();

            var list = await _transactions.GetTransactionSplitsAsync();
            foreach (var s in list)
            {
                Splits.Add(s);
            }

            StatusMessage = list.Count == 0 ? "No transaction splits found" : $"Loaded {list.Count} transaction splits";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading transaction splits: {ex.Message}";
        }
    }

    /// <summary>
    /// Load transaction splits for a specific allocation.
    /// Uses the existing /transactionsplits?AllocationId=... endpoint.
    /// </summary>
    public async Task LoadTransactionSplitsForAllocationAsync(int allocationId)
    {
        if (!IsLoggedIn) return;

        try
        {
            StatusMessage = "Loading transaction splits...";
            Splits.Clear();

            var list = await _transactions.GetTransactionSplitsForAllocationAsync(allocationId);
            foreach (var s in list)
            {
                Splits.Add(s);
            }

            StatusMessage = list.Count == 0 ? "No transaction splits found" : $"Loaded {list.Count} transaction splits";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading transaction splits: {ex.Message}";
        }
    }

    /// <summary>
    /// Refresh transactions by checking session and reloading data.
    /// </summary>
    public async Task RefreshAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadTransactionsAsync();
        }
    }
}

/// <summary>
/// Item model for displaying transaction information in the UI.
/// Represents a flattened view of transaction data with computed category display.
/// </summary>
public sealed record TransactionItem(
    int Id,
    DateTime Date,
    string Payee,
    string Description,
    decimal Amount,
    string AccountName,
    string Categories);
