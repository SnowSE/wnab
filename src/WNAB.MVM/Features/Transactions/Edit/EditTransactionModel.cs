using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

public partial class EditTransactionModel : ObservableObject
{
    private readonly TransactionManagementService _transactions;
    private readonly AccountManagementService _accounts;
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

    public EditTransactionModel(
        TransactionManagementService transactions,
        AccountManagementService accounts,
        IAuthenticationService authService,
        IBudgetSnapshotService budgetSnapshotService)
    {
        _transactions = transactions;
        _accounts = accounts;
        _authService = authService;
        _budgetSnapshotService = budgetSnapshotService;
    }

    public async Task InitializeAsync()
    {
      await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadAccountsAsync();
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

public void Clear()
    {
 TransactionId = 0;
   Payee = string.Empty;
        Description = string.Empty;
        Amount = 0;
 SelectedAccount = null;
      TransactionDate = DateTime.Today;
     IsReconciled = false;
StatusMessage = string.Empty;
    }
}
