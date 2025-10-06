namespace WNAB.Maui;

public interface IPopupService
{
    Task ShowNewTransactionAsync();
    Task ShowAddCategoryAsync();
    // LLM-Dev:v2 Add popup entry points for Users and Accounts
    Task ShowAddUserAsync();
    Task ShowAddAccountAsync();
    // LLM-Dev:v3 Add popup entry point for Login
    Task ShowLoginAsync();
}
