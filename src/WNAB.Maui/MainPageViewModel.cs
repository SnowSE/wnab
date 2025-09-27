using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

public partial class MainPageViewModel : ObservableObject
{
    private readonly IPopupService _popupService;

    public MainPageViewModel(IPopupService popupService)
    {
        _popupService = popupService;
    }

    [RelayCommand]
    private async Task OpenNewTransaction()
    {
        await _popupService.ShowNewTransactionAsync();
    }

    [RelayCommand]
    private async Task OpenAddCategory()
    {
        await _popupService.ShowAddCategoryAsync();
    }

    [RelayCommand]
    private async Task OpenAddUser()
    {
        await _popupService.ShowAddUserAsync();
    }

    [RelayCommand]
    private async Task OpenAddAccount()
    {
        await _popupService.ShowAddAccountAsync();
    }
}
