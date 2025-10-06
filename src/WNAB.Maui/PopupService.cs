using CommunityToolkit.Maui.Views;

namespace WNAB.Maui;

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
        var popup = _services.GetRequiredService<TransactionPopup>();
        // Need a current page to display from; use Application.Current.MainPage
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            await page.ShowPopupAsync(popup);
        }
    }

    public async Task ShowAddCategoryAsync()
    {
        var popup = _services.GetRequiredService<AddCategoryPopup>();
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            await page.ShowPopupAsync(popup);
        }
    }

    public async Task ShowAddUserAsync()
    {
        var popup = _services.GetRequiredService<AddUserPopup>();
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            await page.ShowPopupAsync(popup);
        }
    }

    public async Task ShowAddAccountAsync()
    {
        var popup = _services.GetRequiredService<AddAccountPopup>();
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            await page.ShowPopupAsync(popup);
        }
    }

    public async Task ShowLoginAsync()
    {
        var popup = _services.GetRequiredService<LoginPage>();
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            await page.ShowPopupAsync(popup);
        }
    }
}
