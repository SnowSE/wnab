namespace WNAB.MVM;

public interface IMVMPopupService
{
    Task ShowNewTransactionAsync();
    Task ShowAddCategoryAsync();
    Task ShowEditCategoryAsync(int categoryId, string name, string? color, bool isActive);
    Task ShowAddAccountAsync();
    Task DisplayAlertAsync(string title, string message);
    Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel);
}
