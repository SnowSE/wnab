namespace WNAB.MVM;

public interface IMVMPopupService
{
    Task ShowNewTransactionAsync();
    Task ShowAddCategoryAsync();
    Task ShowAddUserAsync();
    Task ShowAddAccountAsync();
}
