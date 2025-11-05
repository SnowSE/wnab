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
    /// Get splits for a specific transaction ID.
    /// </summary>
    public IEnumerable<TransactionSplitResponse> GetSplitsForTransaction(int transactionId)
    {
        // Force a new query each time to avoid caching issues
        return Splits.Where(s => s.TransactionId == transactionId).OrderBy(s => s.CategoryName).ToList();
    }

    /// <summary>
    /// Initialize the model by checking user session and loading transactions if authenticated.
    /// </summary>
    public async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
    await LoadTransactionsAndSplitsAsync();
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
  /// Load both transactions and splits in parallel.
  /// Automatically called on initialize and refresh.
    /// </summary>
    public async Task LoadTransactionsAndSplitsAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[TransactionsModel] LoadTransactionsAndSplitsAsync called. IsBusy={IsBusy}, IsLoggedIn={IsLoggedIn}");
        
        if (IsBusy || !IsLoggedIn)
        {
            System.Diagnostics.Debug.WriteLine($"[TransactionsModel] Skipping load - busy or not logged in");
            return;
        }

        try
        {
   IsBusy = true;
   StatusMessage = "Loading transactions and splits...";

    // Load both in parallel for better performance
      var transactionsTask = _transactions.GetTransactionsForUserAsync();
            var splitsTask = _transactions.GetTransactionSplitsAsync();

   await Task.WhenAll(transactionsTask, splitsTask);

        var transactionsList = transactionsTask.Result;
      var splitsList = splitsTask.Result;
      
            System.Diagnostics.Debug.WriteLine($"[TransactionsModel] Loaded {transactionsList.Count} transactions and {splitsList.Count} splits from API");

            // Clear collections
            Items.Clear();
            Splits.Clear();

            // Populate Items collection (sorted by date descending)
            foreach (var t in transactionsList.OrderByDescending(t => t.TransactionDate))
  {
     Items.Add(new TransactionItem(
        t.Id,
         t.TransactionDate,
                    t.Payee,
           t.Description,
      t.Amount,
       t.AccountName));
     }

    // Populate Splits collection
       foreach (var s in splitsList)
     {
                Splits.Add(s);
            }
            
            System.Diagnostics.Debug.WriteLine($"[TransactionsModel] Collections populated. Items.Count={Items.Count}, Splits.Count={Splits.Count}");

            StatusMessage = transactionsList.Count == 0 
                ? "No transactions found" 
                : $"Loaded {transactionsList.Count} transactions and {splitsList.Count} splits";
            
            // Notify property changed to ensure UI updates
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(Splits));
            
            System.Diagnostics.Debug.WriteLine($"[TransactionsModel] PropertyChanged notifications sent");
   }
   catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TransactionsModel] ERROR loading data: {ex.Message}");
       StatusMessage = $"Error loading data: {ex.Message}";
        }
        finally
        {
      IsBusy = false;
 }
    }

 /// <summary>
    /// Load transactions for the current authenticated user (using API response records).
    /// Note: Use LoadTransactionsAndSplitsAsync() instead to load both transactions and splits.
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
              Items.Add(new TransactionItem(
        t.Id,
       t.TransactionDate,
      t.Payee,
        t.Description,
        t.Amount,
        t.AccountName));
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
    /// Delete a transaction and refresh the list.
    /// </summary>
    public async Task<(bool success, string message)> DeleteTransactionAsync(int transactionId)
    {
        if (!IsLoggedIn)
            return (false, "Please log in first");

        try
        {
            IsBusy = true;
            StatusMessage = "Deleting transaction...";

            await _transactions.DeleteTransactionAsync(transactionId);

            StatusMessage = "Transaction deleted successfully";
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error deleting transaction: {ex.Message}";
            StatusMessage = errorMsg;
            return (false, errorMsg);
        }
        finally
        {
            IsBusy = false;
        }
        
        // Refresh AFTER IsBusy is set to false
        await LoadTransactionsAndSplitsAsync();
        
        return (true, "Transaction deleted successfully");
    }

    /// <summary>
    /// Delete a transaction split and refresh the list.
    /// </summary>
    public async Task<(bool success, string message)> DeleteTransactionSplitAsync(int splitId)
    {
        System.Diagnostics.Debug.WriteLine($"[TransactionsModel] DeleteTransactionSplitAsync called for split ID: {splitId}");
        
        if (!IsLoggedIn)
        {
            System.Diagnostics.Debug.WriteLine($"[TransactionsModel] User not logged in, aborting delete");
            return (false, "Please log in first");
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Deleting transaction split...";
            
            System.Diagnostics.Debug.WriteLine($"[TransactionsModel] Calling API to delete split {splitId}");
            await _transactions.DeleteTransactionSplitAsync(splitId);
            
            System.Diagnostics.Debug.WriteLine($"[TransactionsModel] API delete succeeded, now refreshing data");
            StatusMessage = "Transaction split deleted successfully";
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error deleting transaction split: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[TransactionsModel] ERROR: {errorMsg}");
            StatusMessage = errorMsg;
            return (false, errorMsg);
        }
        finally
        {
            IsBusy = false;
        }
        
        // Refresh AFTER IsBusy is set to false
        System.Diagnostics.Debug.WriteLine($"[TransactionsModel] Now calling refresh with IsBusy=false");
        await LoadTransactionsAndSplitsAsync();
        System.Diagnostics.Debug.WriteLine($"[TransactionsModel] Refresh complete");
        
        return (true, "Transaction split deleted successfully");
    }

    /// <summary>
    /// Refresh transactions and splits by checking session and reloading data.
    /// </summary>
    public async Task RefreshAsync()
    {
     await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
        await LoadTransactionsAndSplitsAsync();     
        }
    }
}

/// <summary>
/// Item model for displaying transaction information in the UI.
/// Represents a flattened view of transaction data.
/// </summary>
public sealed record TransactionItem(
    int Id,
    DateTime Date,
    string Payee,
    string Description,
    decimal Amount,
    string AccountName
);
