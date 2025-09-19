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
}

public interface IPopupService
{
    Task ShowNewTransactionAsync();
}

internal class PopupService : IPopupService
{
    private readonly IServiceProvider _services;

    public PopupService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task ShowNewTransactionAsync()
    {
        // Resolve popup (allows DI into popup later)
        var popup = _services.GetRequiredService<NewTransactionPopup>();
        // Need a current page to display from; use Application.Current.MainPage
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            await page.ShowPopupAsync(popup);
        }
    }
}
