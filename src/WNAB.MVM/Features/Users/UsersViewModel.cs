using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for UsersPage - thin coordination layer between View and Model.
/// Handles UI-specific concerns like navigation and popups, delegates business logic to Model.
/// </summary>
public partial class UsersViewModel : ObservableObject
{
    private readonly IMVMPopupService _popupService;

    public UsersModel Model { get; }

    public UsersViewModel(UsersModel model, IMVMPopupService popupService)
    {
        Model = model;
        _popupService = popupService;
    }

    /// <summary>
    /// Initialize the ViewModel by delegating to the Model.
    /// </summary>
    public async Task InitializeAsync()
    {
        await Model.LoadUsersAsync();
    }

    /// <summary>
    /// Add User command - shows popup then refreshes the list.
    /// Pure UI coordination - shows popup and triggers refresh.
    /// </summary>
    [RelayCommand]
    private async Task AddUser()
    {
        await _popupService.ShowAddUserAsync();
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
