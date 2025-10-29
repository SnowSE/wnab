using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

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
    /// Load transactions for the current authenticated user (using DTOs).
    /// Transforms DTOs into TransactionItem view models with category information.
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
                // DTO has CategoryName directly in TransactionSplits
                var categoryNames = t.TransactionSplits.Select(ts => ts.CategoryName ?? "Unknown").ToList();
                var categoriesText = categoryNames.Count > 1
                    ? $"{categoryNames.Count} categories"
                    : categoryNames.FirstOrDefault() ?? "No category";

                Items.Add(new TransactionItem(
                    t.Id,
                    t.TransactionDate,
                    t.Payee,
                    t.Description,
                    t.Amount,
                    t.AccountName, // DTO has AccountName directly
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
