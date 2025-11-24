using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

public partial class EditTransactionModel : ObservableObject
{
    private readonly TransactionManagementService _transactions;
    private readonly AccountManagementService _accounts;
    private readonly CategoryManagementService _categories;
    private readonly IAuthenticationService _authService;
    private readonly IBudgetSnapshotService _budgetSnapshotService;

    [ObservableProperty]
    private int transactionId;

    [ObservableProperty]
    private int accountId;

    [ObservableProperty]
    private string payee = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private DateTime transactionDate = DateTime.Today;

    [ObservableProperty]
    private bool isReconciled;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private Account? selectedAccount;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
  private bool isBusy;

    public ObservableCollection<Account> AvailableAccounts { get; } = new();
    public ObservableCollection<Category> AvailableCategories { get; } = new();
    public ObservableCollection<EditableSplitItem> Splits { get; } = new();

    public EditTransactionModel(
        TransactionManagementService transactions,
        AccountManagementService accounts,
        CategoryManagementService categories,
        IAuthenticationService authService,
        IBudgetSnapshotService budgetSnapshotService)
    {
        _transactions = transactions;
        _accounts = accounts;
        _categories = categories;
        _authService = authService;
        _budgetSnapshotService = budgetSnapshotService;
    }

    public async Task InitializeAsync()
    {
      await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadAccountsAsync();
            await LoadCategoriesAsync();
        }
    }

    public async Task LoadTransactionAsync(int id)
    {
        TransactionId = id;
        
        try
        {
            var transaction = await _transactions.GetTransactionByIdAsync(id);
          
      if (transaction == null)
     {
     StatusMessage = "Transaction not found";
         return;
            }

            AccountId = transaction.AccountId;
    Payee = transaction.Payee;
  Description = transaction.Description;
       Amount = transaction.Amount;
         TransactionDate = transaction.TransactionDate;
       IsReconciled = transaction.IsReconciled;

      SelectedAccount = AvailableAccounts.FirstOrDefault(a => a.Id == AccountId);
      
            // Load splits for this transaction
            await LoadSplitsAsync(id);
      
   StatusMessage = "Ready to edit transaction";
        }
  catch (Exception ex)
  {
            StatusMessage = $"Error loading transaction: {ex.Message}";
        }
    }

    private async Task CheckUserSessionAsync()
    {
        try
{
   IsLoggedIn = await _authService.IsAuthenticatedAsync();
            if (!IsLoggedIn)
    {
                StatusMessage = "Please log in first";
 }
        }
        catch (Exception ex)
   {
            IsLoggedIn = false;
            StatusMessage = $"Error checking login: {ex.Message}";
}
    }

    private async Task LoadAccountsAsync()
  {
        try
      {
            var accounts = await _accounts.GetAccountsForUserAsync();
         AvailableAccounts.Clear();
         foreach (var account in accounts)
                AvailableAccounts.Add(account);
}
        catch (Exception ex)
        {
   StatusMessage = $"Error loading accounts: {ex.Message}";
      }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _categories.GetCategoriesForUserAsync();
            AvailableCategories.Clear();
            foreach (var category in categories)
                AvailableCategories.Add(category);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading categories: {ex.Message}";
        }
    }

    private async Task LoadSplitsAsync(int transactionId)
    {
        try
        {
            var splits = await _transactions.GetTransactionSplitsAsync();
            var transactionSplits = splits.Where(s => s.TransactionId == transactionId).ToList();
            
            Splits.Clear();
            foreach (var split in transactionSplits)
            {
                var category = AvailableCategories.FirstOrDefault(c => c.Name == split.CategoryName);
                Splits.Add(new EditableSplitItem(
                    split.Id,
                    split.CategoryAllocationId,
                    category,
                    split.Amount,
                    split.Description));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading splits: {ex.Message}";
        }
    }

    public void AddNewSplit()
    {
        Splits.Add(new EditableSplitItem());
    }

    public void RemoveSplit(EditableSplitItem split)
    {
        Splits.Remove(split);
    }

    public async Task<bool> DeleteSplitAsync(EditableSplitItem split)
    {
        if (split.IsNew)
        {
            Splits.Remove(split);
            return true;
        }

        try
        {
            await _transactions.DeleteTransactionSplitAsync(split.Id);
            Splits.Remove(split);
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting split: {ex.Message}";
            return false;
        }
    }

    partial void OnSelectedAccountChanged(Account? value)
    {
        AccountId = value?.Id ?? 0;
    }

    public string? ValidateForSave()
    {
      if (!IsLoggedIn)
    return "Please log in first";

 if (AccountId <= 0)
       return "Please select an account";

      if (string.IsNullOrWhiteSpace(Payee))
          return "Please enter a payee";

        if (Amount == 0)
            return "Please enter an amount";

        return null;
    }

    public async Task<(bool success, string message)> UpdateTransactionAsync()
    {
        var validationError = ValidateForSave();
        if (validationError != null)
        {
            StatusMessage = validationError;
  return (false, validationError);
        }

        try
        {
   IsBusy = true;
    StatusMessage = "Updating transaction...";

            var utcTransactionDate = DateTime.SpecifyKind(TransactionDate, DateTimeKind.Utc);

            var request = new EditTransactionRequest(
                TransactionId,
     AccountId,
                Payee,
         Description,
                Amount,
       utcTransactionDate,
      IsReconciled
            );

            await _transactions.UpdateTransactionAsync(request);
            
            // Update or create splits
            await UpdateSplitsAsync();
            
            // Invalidate snapshots from this transaction's month forward
            await _budgetSnapshotService.InvalidateSnapshotsFromMonthAsync(TransactionDate.Month, TransactionDate.Year);
            
            StatusMessage = "Transaction updated successfully!";

            return (true, "Transaction updated successfully!");
        }
        catch (Exception ex)
 {
         var errorMsg = $"Error updating transaction: {ex.Message}";
            StatusMessage = errorMsg;
   return (false, errorMsg);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateSplitsAsync()
    {
        foreach (var split in Splits)
        {
            if (split.IsNew && split.Amount != 0)
            {
                // Create new split
                var createRequest = new TransactionSplitRecord(
                    split.CategoryAllocationId,
                    TransactionId,
                    split.Amount,
                    split.Description);
                await _transactions.CreateTransactionSplitAsync(createRequest);
            }
            else if (!split.IsNew)
            {
                // Update existing split
                var updateRequest = new EditTransactionSplitRequest(
                    split.Id,
                    split.CategoryAllocationId,
                    split.Amount,
                    split.Description);
                await _transactions.UpdateTransactionSplitAsync(updateRequest);
            }
        }
    }

public void Clear()
    {
 TransactionId = 0;
   Payee = string.Empty;
        Description = string.Empty;
        Amount = 0;
 SelectedAccount = null;
      TransactionDate = DateTime.Today;
     IsReconciled = false;
        Splits.Clear();
StatusMessage = string.Empty;
    }
}
