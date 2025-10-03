using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic;
using WNAB.Logic.Data;
using Microsoft.Maui.Storage;

namespace WNAB.Maui;

// LLM-Dev:v2 Enhanced with account/category loading, date picker, and proper validation feedback
public partial class TransactionViewModel : ObservableObject
{
    private readonly TransactionManagementService _transactions;
    private readonly AccountManagementService _accounts;
    private readonly CategoryManagementService _categories;

    public event EventHandler? RequestClose; // Raised to close popup

    [ObservableProperty]
    private int accountId;

    [ObservableProperty]
    private string payee = string.Empty;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private string memo = string.Empty;

    [ObservableProperty]
    private int categoryId;

    [ObservableProperty]
    private DateTime transactionDate = DateTime.Today;

    [ObservableProperty]
    private string statusMessage = "Loading...";

    [ObservableProperty]
    private Account? selectedAccount;

    [ObservableProperty]
    private Category? selectedCategory;

    // LLM-Dev:v2 Observable collections for pickers
    public ObservableCollection<Account> AvailableAccounts { get; } = new();
    public ObservableCollection<Category> AvailableCategories { get; } = new();

    private int _userId;
    private bool _isLoggedIn;

    public TransactionViewModel(
        TransactionManagementService transactions, 
        AccountManagementService accounts,
        CategoryManagementService categories)
    {
        _transactions = transactions;
        _accounts = accounts;
        _categories = categories;
    }

    // LLM-Dev:v2 Initialize by loading user session and available accounts/categories
    public async Task InitializeAsync()
    {
        try
        {
            var userIdString = await SecureStorage.Default.GetAsync("userId");
            if (!string.IsNullOrWhiteSpace(userIdString) && int.TryParse(userIdString, out var parsedUserId))
            {
                _userId = parsedUserId;
                _isLoggedIn = true;
                await LoadDataAsync();
            }
            else
            {
                _isLoggedIn = false;
                StatusMessage = "Please log in first";
            }
        }
        catch (Exception ex)
        {
            _isLoggedIn = false;
            StatusMessage = $"Error checking login: {ex.Message}";
        }
    }

    // LLM-Dev:v2 Load accounts and categories for the logged-in user
    private async Task LoadDataAsync()
    {
        try
        {
            StatusMessage = "Loading...";
            
            var accountsTask = _accounts.GetAccountsForUserAsync(_userId);
            var categoriesTask = _categories.GetCategoriesForUserAsync(_userId);

            await Task.WhenAll(accountsTask, categoriesTask);

            AvailableAccounts.Clear();
            foreach (var account in accountsTask.Result)
                AvailableAccounts.Add(account);

            AvailableCategories.Clear();
            foreach (var category in categoriesTask.Result)
                AvailableCategories.Add(category);

            if (AvailableAccounts.Count == 0)
            {
                StatusMessage = "No accounts found. Please create an account first.";
            }
            else if (AvailableCategories.Count == 0)
            {
                StatusMessage = "No categories found. Please create a category first.";
            }
            else
            {
                StatusMessage = "Ready to create transaction";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading data: {ex.Message}";
        }
    }

    // LLM-Dev:v2 Update AccountId when account is selected
    partial void OnSelectedAccountChanged(Account? value)
    {
        AccountId = value?.Id ?? 0;
    }

    // LLM-Dev:v2 Update CategoryId when category is selected
    partial void OnSelectedCategoryChanged(Category? value)
    {
        CategoryId = value?.Id ?? 0;
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task Save()
    {
        // LLM-Dev:v2 Enhanced validation with user feedback
        if (!_isLoggedIn || _userId <= 0)
        {
            StatusMessage = "Please log in first";
            return;
        }

        if (AccountId <= 0)
        {
            StatusMessage = "Please select an account";
            return;
        }

        if (string.IsNullOrWhiteSpace(Payee))
        {
            StatusMessage = "Please enter a payee";
            return;
        }

        if (Amount == 0)
        {
            StatusMessage = "Please enter an amount";
            return;
        }

        if (CategoryId <= 0)
        {
            StatusMessage = "Please select a category";
            return;
        }

        try
        {
            StatusMessage = "Creating transaction...";
            
            // LLM-Dev:v3 Convert selected date to UTC for PostgreSQL compatibility
            // DatePicker returns local DateTime, but PostgreSQL requires UTC for timestamp with time zone
            var utcTransactionDate = DateTime.SpecifyKind(TransactionDate, DateTimeKind.Utc);
            
            var record = TransactionManagementService.CreateSimpleTransactionRecord(
                AccountId, Payee, Memo, Amount, utcTransactionDate, CategoryId);
            
            await _transactions.CreateTransactionAsync(record);
            StatusMessage = "Transaction created successfully!";

            // LLM-Dev:v2 Clear fields for next use
            Payee = string.Empty;
            Memo = string.Empty;
            Amount = 0;
            SelectedAccount = null;
            SelectedCategory = null;
            TransactionDate = DateTime.Today;

            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating transaction: {ex.Message}";
        }
    }
}