using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic;
using WNAB.Logic.Data;

namespace WNAB.Maui;

public partial class TransactionViewModel : ObservableObject
{
    private readonly TransactionManagementService _transactions;

    [ObservableProperty]
    private int accountId;

    [ObservableProperty]
    private string payee = string.Empty;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private string memo = string.Empty;

    [ObservableProperty]
    private int categoryId;

    public event EventHandler? RequestClose; // Raised to close popup

    public TransactionViewModel(TransactionManagementService transactions)
    {
        _transactions = transactions;
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task Save()
    {
        // LLM-Dev: Basic validation and transaction creation
        if (AccountId <= 0 || string.IsNullOrWhiteSpace(Payee) || Amount == 0 || CategoryId <= 0)
            return;

        try
        {
            var record = TransactionManagementService.CreateSimpleTransactionRecord(
                AccountId, Payee, Memo, Amount, DateTime.Now, CategoryId);
            
            await _transactions.CreateTransactionAsync(record);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            // TODO: Handle error appropriately (show message, log, etc.)
            // For now, just don't close the popup
        }
    }
}