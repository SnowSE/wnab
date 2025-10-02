using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;

namespace WNAB.Maui;

internal class PopupService : IPopupService
{
    private readonly IServiceProvider _services;

    public PopupService(IServiceProvider services)
    {
        _services = services;
    }

    public Task ShowNewTransactionAsync()
    {
        // TODO: The CommunityToolkit.Maui v12 API changed - needs updating
        // Temporarily returning completed task until popup API is fixed
        return Task.CompletedTask;

        // Resolve popup (allows DI into popup later)
        //var popup = _services.GetRequiredService<NewTransactionPopup>();
        //return MainThread.InvokeOnMainThreadAsync(async () =>
        //{
        //    var page = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
        //    if (page is not null)
        //    {
        //        await page.ShowPopupAsync(popup);
        //    }
        //});
    }

    public Task ShowAddCategoryAsync()
    {
        // TODO: The CommunityToolkit.Maui v12 API changed - needs updating
        return Task.CompletedTask;
    }

    public Task ShowAddUserAsync()
    {
        // TODO: The CommunityToolkit.Maui v12 API changed - needs updating
        return Task.CompletedTask;
    }

    public Task ShowAddAccountAsync()
    {
        // TODO: The CommunityToolkit.Maui v12 API changed - needs updating
        return Task.CompletedTask;
    }
}
