namespace WNAB.Maui;

public class PopupService(TransactionPopup transactionPopup, AddCategoryPopup addCategoryPopup, EditCategoryPopup editCategoryPopup, AddAccountPopup addAccountPopup) : IMVMPopupService
{
    public async Task ShowNewTransactionAsync()
    {
        // Need a current page to display from; use Application.Current.MainPage
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            await page.ShowPopupAsync(transactionPopup);
        }
    }

    public async Task ShowAddCategoryAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            await page.ShowPopupAsync(addCategoryPopup);
        }
    }

    public async Task ShowEditCategoryAsync(int categoryId, string name, string? color, bool isActive)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            editCategoryPopup.Initialize(categoryId, name, color, isActive);
            await page.ShowPopupAsync(editCategoryPopup);
        }
    }

    public async Task ShowAddAccountAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            await page.ShowPopupAsync(addAccountPopup);
        }
    }
}
