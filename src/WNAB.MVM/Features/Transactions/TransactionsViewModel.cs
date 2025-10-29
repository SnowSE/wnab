using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for TransactionsPage - thin coordination layer between View and Model.
/// Handles UI-specific concerns like navigation and popups, delegates business logic to Model.
/// </summary>
public partial class TransactionsViewModel : ObservableObject
{
    private readonly IMVMPopupService _popupService;

    public TransactionsModel Model { get; }

    public TransactionsViewModel(TransactionsModel model, IMVMPopupService popupService)
    {
        Model = model;
        _popupService = popupService;
    }

    /// <summary>
    /// Initialize the ViewModel by delegating to the Model.
    /// </summary>
    public async Task InitializeAsync()
    {
        await Model.InitializeAsync();
    }

    /// <summary>
    /// Refresh command - delegates to Model.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await Model.RefreshAsync();
    }

    /// <summary>
    /// Add Transaction command - shows popup then refreshes the list.
    /// Pure UI coordination - shows popup and triggers refresh.
    /// </summary>
    [RelayCommand]
    private async Task AddTransaction()
    {
        await _popupService.ShowNewTransactionAsync();
        await Model.RefreshAsync();
    }

    /// <summary>
    /// Navigate to Home command - pure navigation logic.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToHome()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
