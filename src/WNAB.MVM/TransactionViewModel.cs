using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using WNAB.Services;

namespace WNAB.MVM;

// LLM-Dev:v2 Enhanced with account/category loading, date picker, and proper validation feedback
public partial class TransactionViewModel : ObservableObject
{
    private readonly TransactionManagementService _transactions;
    private readonly AccountManagementService _accounts;
    private readonly CategoryManagementService _categories;
    private readonly IAuthenticationService _authService;
    private readonly CategoryAllocationManagementService _allocations;

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

    [ObservableProperty]
    private bool isSplitTransaction = false;

    // LLM-Dev:v2 Observable collections for pickers
    public ObservableCollection<Account> AvailableAccounts { get; } = new();
    public ObservableCollection<Category> AvailableCategories { get; } = new();

    // LLM-Dev:v3 Collection for managing transaction splits
    public ObservableCollection<TransactionSplitViewModel> Splits { get; } = new();

    private bool _isLoggedIn;

    public TransactionViewModel(
        TransactionManagementService transactions,
        AccountManagementService accounts,
        CategoryManagementService categories,
        IAuthenticationService authService,
        CategoryAllocationManagementService allocations)
    {
        _transactions = transactions;
        _accounts = accounts;
        _categories = categories;
        _authService = authService;
        _allocations = allocations;
    }

    // LLM-Dev:v2 Initialize by loading user session and available accounts/categories
    public async Task InitializeAsync()
    {
        try
        {
            _isLoggedIn = await _authService.IsAuthenticatedAsync();
            if (_isLoggedIn)
            {
                await LoadDataAsync();
            }
            else
            {
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

            var accountsTask = _accounts.GetAccountsForUserAsync();
            var categoriesTask = _categories.GetCategoriesForUserAsync();

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

    // LLM-Dev:v5 Update CategoryId and find CategoryAllocation when category is selected
    // Enforces budget-first approach: CategoryAllocation must exist for the transaction date
    partial void OnSelectedCategoryChanged(Category? value)
    {
        CategoryId = value?.Id ?? 0;
        
        // LLM-Dev:v5 When category changes, automatically find allocation for transaction date
        if (value != null && !IsSplitTransaction)
        {
            _ = ValidateAndSetCategoryAllocationAsync(value.Id);
        }
    }
    
    // LLM-Dev:v5 Find and validate CategoryAllocation for the selected category and transaction date
    private async Task ValidateAndSetCategoryAllocationAsync(int categoryId)
    {
        try
        {
            var allocation = await _allocations.FindAllocationAsync(
                categoryId, 
                TransactionDate.Month, 
                TransactionDate.Year);
            
            if (allocation == null)
            {
                StatusMessage = $"No budget allocation found for {TransactionDate:MMMM yyyy}. Please create a budget first.";
            }
            else
            {
                StatusMessage = "Ready to create transaction";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error validating category allocation: {ex.Message}";
        }
    }
    
    // LLM-Dev:v5 Re-validate allocation when transaction date changes
    partial void OnTransactionDateChanged(DateTime value)
    {
        if (SelectedCategory != null && !IsSplitTransaction)
        {
            _ = ValidateAndSetCategoryAllocationAsync(SelectedCategory.Id);
        }
    }

    // LLM-Dev:v3 Calculate remaining amount to allocate across splits
    public decimal RemainingAmount
    {
        get
        {
            var splitsTotal = Splits.Sum(s => s.Amount);
            return Amount - splitsTotal;
        }
    }

    // LLM-Dev:v3 Check if splits are balanced with transaction amount
    public bool AreSplitsBalanced => Math.Abs(RemainingAmount) < 0.01m;

    // LLM-Dev:v3 Recalculate split totals when amount changes
    partial void OnAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(RemainingAmount));
        OnPropertyChanged(nameof(AreSplitsBalanced));
    }

    [RelayCommand]
    private void ToggleSplitTransaction()
    {
        IsSplitTransaction = !IsSplitTransaction;
        
        if (IsSplitTransaction)
        {
            // LLM-Dev:v3 Initialize with one split containing the full amount
            if (Splits.Count == 0)
            {
                AddSplit();
            }
        }
    }

    [RelayCommand]
    private void AddSplit()
    {
        // LLM-Dev:v5 Create new split with remaining amount as default
        var newSplit = new TransactionSplitViewModel
        {
            Amount = RemainingAmount > 0 ? RemainingAmount : 0
        };
        
        // LLM-Dev:v5 Subscribe to property changes to update totals and validate allocations
        newSplit.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(TransactionSplitViewModel.Amount))
            {
                OnPropertyChanged(nameof(RemainingAmount));
                OnPropertyChanged(nameof(AreSplitsBalanced));
            }
            else if (e.PropertyName == nameof(TransactionSplitViewModel.SelectedCategory))
            {
                // LLM-Dev:v5 When category is selected in a split, find and set the allocation
                if (newSplit.SelectedCategory != null)
                {
                    await FindAndSetAllocationForSplitAsync(newSplit);
                }
            }
        };
        
        Splits.Add(newSplit);
        OnPropertyChanged(nameof(RemainingAmount));
        OnPropertyChanged(nameof(AreSplitsBalanced));
    }
    
    // LLM-Dev:v5 Find CategoryAllocation for a split based on transaction date
    private async Task FindAndSetAllocationForSplitAsync(TransactionSplitViewModel split)
    {
        if (split.SelectedCategory == null) return;
        
        try
        {
            var allocation = await _allocations.FindAllocationAsync(
                split.SelectedCategory.Id, 
                TransactionDate.Month, 
                TransactionDate.Year);
            
            split.SelectedCategoryAllocation = allocation;
            
            if (allocation == null)
            {
                StatusMessage = $"Missing budget allocation for {split.SelectedCategory.Name} in {TransactionDate:MMMM yyyy}";
            }
            else if (Splits.All(s => s.CategoryAllocationId > 0))
            {
                StatusMessage = "Ready to create transaction";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error finding allocation: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoveSplit(TransactionSplitViewModel split)
    {
        // LLM-Dev:v3 Prevent removing the last split in split mode
        if (Splits.Count > 1)
        {
            Splits.Remove(split);
            OnPropertyChanged(nameof(RemainingAmount));
            OnPropertyChanged(nameof(AreSplitsBalanced));
        }
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
        if (!_isLoggedIn)
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

        // LLM-Dev:v5 Validate category selection (allocation will be checked during save)
        if (!IsSplitTransaction && SelectedCategory == null)
        {
            StatusMessage = "Please select a category";
            return;
        }

        // LLM-Dev:v3 Validate splits if in split mode
        if (IsSplitTransaction)
        {
            if (Splits.Count == 0)
            {
                StatusMessage = "Please add at least one split";
                return;
            }

            if (!AreSplitsBalanced)
            {
                StatusMessage = $"Splits must total transaction amount. Remaining: {RemainingAmount:C}";
                return;
            }

            // LLM-Dev:v4 Validate each split has a category allocation
            if (Splits.Any(s => s.CategoryAllocationId <= 0))
            {
                StatusMessage = "Please select a category for all splits";
                return;
            }
        }

        try
        {
            StatusMessage = "Creating transaction...";
            
            // LLM-Dev:v3 Convert selected date to UTC for PostgreSQL compatibility
            // DatePicker returns local DateTime, but PostgreSQL requires UTC for timestamp with time zone
            var utcTransactionDate = DateTime.SpecifyKind(TransactionDate, DateTimeKind.Utc);
            
            TransactionRecord record;
            
            if (IsSplitTransaction)
            {
                // LLM-Dev:v4 Create transaction with multiple splits using CategoryAllocation
                var splitRecords = Splits.Select(s => 
                    new TransactionSplitRecord(s.CategoryAllocationId, s.Amount, s.IsIncome, s.Notes)).ToList();
                    
                record = TransactionManagementService.CreateTransactionRecord(
                    AccountId, Payee, Memo, Amount, utcTransactionDate, splitRecords);
            }
            else
            {
                // LLM-Dev:v5 For simple transactions, find the CategoryAllocation for the selected category and date
                if (SelectedCategory == null)
                {
                    StatusMessage = "Please select a category";
                    return;
                }
                
                var allocation = await _allocations.FindAllocationAsync(
                    SelectedCategory.Id, 
                    TransactionDate.Month, 
                    TransactionDate.Year);
                
                if (allocation == null)
                {
                    StatusMessage = $"No budget allocation found for {SelectedCategory.Name} in {TransactionDate:MMMM yyyy}. Please create a budget first.";
                    return;
                }
                
                record = TransactionManagementService.CreateSimpleTransactionRecord(
                    AccountId, Payee, Memo, Amount, utcTransactionDate, allocation.Id, SelectedCategory.IsIncome);
            }
            
            await _transactions.CreateTransactionAsync(record);
            StatusMessage = "Transaction created successfully!";

            // LLM-Dev:v2 Clear fields for next use
            Payee = string.Empty;
            Memo = string.Empty;
            Amount = 0;
            SelectedAccount = null;
            SelectedCategory = null;
            TransactionDate = DateTime.Today;
            IsSplitTransaction = false;
            Splits.Clear();

            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating transaction: {ex.Message}";
        }
    }
}