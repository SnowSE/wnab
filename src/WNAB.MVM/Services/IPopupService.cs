namespace WNAB.MVM;

public interface IMVMPopupService
{
    Task ShowNewTransactionAsync();
    Task ShowAddCategoryAsync();
    Task ShowEditCategoryAsync(int categoryId, string name, string? color, bool isActive);
    Task ShowAddAccountAsync();
}
