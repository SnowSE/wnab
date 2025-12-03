using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for Transaction feature.
/// Handles transaction creation, validation, split management, and category allocation enforcement.
/// </summary>
public partial class AddTransactionModel : ObservableObject
{
    private readonly TransactionManagementService _transactions;
    private readonly AccountManagementService _accounts;
    private readonly CategoryManagementService _categories;
    private readonly IAuthenticationService _authService;
    private readonly CategoryAllocationManagementService _allocations;
    private readonly IBudgetSnapshotService _budgetSnapshotService;

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
    private bool isLoggedIn;

    [ObservableProperty]
    private bool isBusy;

    // Observable collections for pickers
    public ObservableCollection<Account> AvailableAccounts { get; } = new();
    public ObservableCollection<Category> AvailableCategories { get; } = new();

    // Collection for managing transaction splits
    public ObservableCollection<AddTransactionSplitViewModel> Splits { get; } = new();

    /// <summary>
    /// Calculate remaining amount to allocate across splits.
    /// </summary>
    public decimal RemainingAmount
    {
        get
        {
            var splitsTotal = Splits.Sum(s => s.Model.Amount);
            return Amount - splitsTotal;
        }
    }

    /// <summary>
    /// Check if splits are balanced with transaction amount.
    /// </summary>
    public bool AreSplitsBalanced => Math.Abs(RemainingAmount) < 0.01m;

    public AddTransactionModel(
        TransactionManagementService transactions,
        AccountManagementService accounts,
        CategoryManagementService categories,
        IAuthenticationService authService,
        CategoryAllocationManagementService allocations,
        IBudgetSnapshotService budgetSnapshotService)
    {
        _transactions = transactions;
        _accounts = accounts;
        _categories = categories;
        _authService = authService;
        _allocations = allocations;
        _budgetSnapshotService = budgetSnapshotService;
    }

    /// <summary>
    /// Initialize by loading user session and available accounts/categories.
    /// </summary>
    public async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadDataAsync();
        }
    }

    /// <summary>
    /// Check if user is logged in and update authentication state.
    /// </summary>
    public async Task CheckUserSessionAsync()
    {
        try
        {
            IsLoggedIn = await _authService.IsAuthenticatedAsync();
            if (!IsLoggedIn)
            {
                StatusMessage = "Please log in first";
            }
        }
        catch (Exception ex)
        {
            IsLoggedIn = false;
            StatusMessage = $"Error checking login: {ex.Message}";
        }
    }

    /// <summary>
    /// Load accounts and categories for the logged-in user.
    /// </summary>
    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Loading...";

            var accountsTask = _accounts.GetAccountsForUserAsync();
            var categoriesTask = _categories.GetCategoriesForUserAsync();

            await Task.WhenAll(accountsTask, categoriesTask);

            AvailableAccounts.Clear();
            foreach (var account in accountsTask.Result)
                AvailableAccounts.Add(account);

            AvailableCategories.Clear();
            // Add Income option at the top for income transactions
            AvailableCategories.Add(new Category { Id = -1, Name = "Income", UserId = 0 });
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
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Update AccountId when account is selected.
    /// </summary>
    partial void OnSelectedAccountChanged(Account? value)
    {
        AccountId = value?.Id ?? 0;
    }

    /// <summary>
    /// Update CategoryId when category is selected.
    /// Enforces budget-first approach: CategoryAllocation must exist for the transaction date.
    /// </summary>
    partial void OnSelectedCategoryChanged(Category? value)
    {
        CategoryId = value?.Id ?? 0;
    }

    /// <summary>
    /// Update SelectedCategory when CategoryId is changed directly (e.g., from Blazor binding).
    /// </summary>
    partial void OnCategoryIdChanged(int value)
    {
        // Handle special values: -1 = Income (null allocation), 0 = No Category (no split)
        if (value == -1)
        {
            // Create a virtual "Income" category for display purposes
            SelectedCategory = new Category { Id = -1, Name = "Income", UserId = 0 };
        }
        else if (value == 0)
        {
            SelectedCategory = null;
        }
        else
        {
            var category = AvailableCategories.FirstOrDefault(c => c.Id == value);
            if (SelectedCategory != category)
            {
                SelectedCategory = category;
            }
        }
    }

    /// <summary>
    /// Recalculate split totals when amount changes.
    /// </summary>
    partial void OnAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(RemainingAmount));
        OnPropertyChanged(nameof(AreSplitsBalanced));
    }

    /// <summary>
    /// Ensure at least one split exists. Called during initialization.
    /// </summary>
    public void EnsureOneSplit()
    {
        if (Splits.Count == 0)
        {
            AddSplit();
        }
    }

    /// <summary>
    /// Add a new split to the transaction.
    /// </summary>
    public void AddSplit()
    {
        // Create new split with remaining amount as default
        var newSplit = new AddTransactionSplitViewModel();
        newSplit.Model.Amount = RemainingAmount > 0 ? RemainingAmount : 0;
        
        // Subscribe to amount changes to update totals
        newSplit.Model.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AddTransactionSplitModel.Amount))
            {
                OnPropertyChanged(nameof(RemainingAmount));
                OnPropertyChanged(nameof(AreSplitsBalanced));
            }
        };
        
        Splits.Add(newSplit);
        OnPropertyChanged(nameof(RemainingAmount));
        OnPropertyChanged(nameof(AreSplitsBalanced));
    }

    /// <summary>
    /// Remove a split from the transaction.
    /// Prevent removing the last split in split mode.
    /// </summary>
    public void RemoveSplit(AddTransactionSplitViewModel split)
    {
        if (Splits.Count > 1)
        {
            Splits.Remove(split);
            OnPropertyChanged(nameof(RemainingAmount));
            OnPropertyChanged(nameof(AreSplitsBalanced));
        }
    }

    /// <summary>
    /// Validate all fields for transaction creation.
    /// Returns error message if validation fails, null if valid.
    /// </summary>
    public string? ValidateForSave()
    {
        if (!IsLoggedIn)
            return "Please log in first";

        if (AccountId <= 0)
            return "Please select an account";

        if (string.IsNullOrWhiteSpace(Payee))
            return "Please enter a payee";

        if (Amount == 0)
            return "Please enter an amount";

        // Always validate splits (transactions always use splits now)
        if (Splits.Count == 0)
            return "Please add at least one split";

        if (!AreSplitsBalanced)
            return $"Splits must total transaction amount. Remaining: {RemainingAmount:C}";

        // Validate each split has a category selected
        if (Splits.Any(s => s.Model.SelectedCategory == null))
            return "Please select a category for all splits";

        return null; // Valid
    }

    /// <summary>
    /// Create the transaction by building the record and calling the service.
    /// Returns success message if created, error message if failed.
    /// </summary>
    public async Task<(bool success, string message)> CreateTransactionAsync()
    {
        // Validate first
        var validationError = ValidateForSave();
        if (validationError != null)
        {
            StatusMessage = validationError;
            return (false, validationError);
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Creating transaction...";
            
            // Convert selected date to UTC for PostgreSQL compatibility
            var utcTransactionDate = DateTime.SpecifyKind(TransactionDate, DateTimeKind.Utc);
            
            // Always use splits - look up allocations for each split at save time
            var splitRecords = new List<TransactionSplitRecord>();
            
            foreach (var split in Splits)
            {
                if (split.Model.SelectedCategory == null)
                {
                    StatusMessage = "Please select a category for all splits";
                    return (false, "Please select a category for all splits");
                }
                
                // Check if this is an Income split (Id == -1)
                if (split.Model.SelectedCategory.Id == -1)
                {
                    // Income: Create split with null CategoryAllocationId
                    splitRecords.Add(new TransactionSplitRecord(
                        null, 
                        -1, 
                        split.Model.Amount, 
                        split.Model.Notes));
                }
                else
                {
                    var allocation = await _allocations.FindAllocationAsync(
                        split.Model.SelectedCategory.Id, 
                        TransactionDate.Month, 
                        TransactionDate.Year);
                    
                    if (allocation == null)
                    {
                        var errorMsg = $"No budget allocation found for {split.Model.SelectedCategory.Name} in {TransactionDate:MMMM yyyy}. Please create a budget first.";
                        StatusMessage = errorMsg;
                        return (false, errorMsg);
                    }
                    
                    splitRecords.Add(new TransactionSplitRecord(
                        allocation.Id, 
                        -1, 
                        split.Model.Amount, 
                        split.Model.Notes));
                }
            }
                
            var record = new TransactionRecord(
                AccountId, Payee, Memo, Amount, utcTransactionDate, splitRecords);
            
            await _transactions.CreateTransactionAsync(record);
            
            // Invalidate snapshots from this transaction's month forward
            await _budgetSnapshotService.InvalidateSnapshotsFromMonthAsync(TransactionDate.Month, TransactionDate.Year);
            
            StatusMessage = "Transaction created successfully!";
            
            return (true, "Transaction created successfully!");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error creating transaction: {ex.Message}";
            StatusMessage = errorMsg;
            return (false, errorMsg);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Clear all form fields for next use.
    /// </summary>
    public void Clear()
    {
        Payee = string.Empty;
        Memo = string.Empty;
        Amount = 0;
        SelectedAccount = null;
        SelectedCategory = null;
        TransactionDate = DateTime.Today;
        Splits.Clear();
        // Initialize with one empty split
        EnsureOneSplit();
        StatusMessage = "Ready to create transaction";
    }
}
