using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for AccountsPage - thin coordination layer between View and Model.
/// Handles UI-specific concerns like navigation and popups, delegates business logic to Model.
/// </summary>
public partial class AccountsViewModel : ObservableObject
{
    private readonly IMVMPopupService _popupService;

    public AccountsModel Model { get; }

    public AccountsViewModel(AccountsModel model, IMVMPopupService popupService)
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
    /// Add Account command - shows popup then refreshes the list.
    /// Pure UI coordination - shows popup and triggers refresh.
    /// </summary>
    [RelayCommand]
    private async Task AddAccount()
    {
        await _popupService.ShowAddAccountAsync();
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

    /// <summary>
    /// Start editing an account - sets the item into edit mode.
    /// </summary>
    [RelayCommand]
    private void EditAccount(AccountItemViewModel accountItem)
    {
        // Cancel any other account that might be in edit mode
        foreach (var item in Model.Items)
        {
            if (item.IsEditing && item != accountItem)
            {
                item.CancelEditing();
            }
        }
        
        accountItem.StartEditing();
    }

    /// <summary>
    /// Save the edited account - calls the API and updates the model.
    /// </summary>
    [RelayCommand]
    private async Task SaveAccount(AccountItemViewModel accountItem)
    {
        if (string.IsNullOrWhiteSpace(accountItem.EditAccountName) || 
            string.IsNullOrWhiteSpace(accountItem.EditAccountType))
        {
            // Could show an error message via popup service if needed
            return;
        }

        var success = await Model.UpdateAccountAsync(
            accountItem.Id, 
            accountItem.EditAccountName, 
            accountItem.EditAccountType);

        if (success)
        {
            accountItem.ApplyChanges();
        }
        else
        {
            // Could show error message via popup service
            accountItem.CancelEditing();
        }
    }

    /// <summary>
    /// Cancel editing an account - discards changes.
    /// </summary>
    [RelayCommand]
    private void CancelEditAccount(AccountItemViewModel accountItem)
    {
        accountItem.CancelEditing();
    }
}
