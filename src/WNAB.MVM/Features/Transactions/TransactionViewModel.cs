using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for Transaction creation - thin coordination layer between View and Model.
/// Handles UI-specific concerns like popup closing, delegates business logic to Model.
/// </summary>
public partial class TransactionViewModel : ObservableObject
{
    public TransactionModel Model { get; }

    public event EventHandler? RequestClose; // Raised to close popup

    public TransactionViewModel(TransactionModel model)
    {
        Model = model;
    }

    /// <summary>
    /// Initialize the ViewModel by delegating to the Model.
    /// </summary>
    public async Task InitializeAsync()
    {
        await Model.InitializeAsync();
    }

    /// <summary>
    /// Toggle split transaction mode - delegates to Model.
    /// </summary>
    [RelayCommand]
    private void ToggleSplitTransaction()
    {
        Model.ToggleSplitTransaction();
    }

    /// <summary>
    /// Add a new split - delegates to Model.
    /// </summary>
    [RelayCommand]
    private void AddSplit()
    {
        Model.AddSplit();
    }

    /// <summary>
    /// Remove a split - delegates to Model.
    /// </summary>
    [RelayCommand]
    private void RemoveSplit(TransactionSplitViewModel split)
    {
        Model.RemoveSplit(split);
    }

    /// <summary>
    /// Cancel button - pure UI action to close popup.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Save transaction - delegates to Model then closes popup on success.
    /// Pure UI coordination - Model handles all business logic.
    /// </summary>
    [RelayCommand]
    private async Task Save()
    {
        var (success, message) = await Model.CreateTransactionAsync();
        
        if (success)
        {
            Model.Clear();
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        // Model already set StatusMessage for errors, no need to do anything else
    }
}
