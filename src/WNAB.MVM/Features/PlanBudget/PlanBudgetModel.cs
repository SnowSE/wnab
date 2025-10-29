using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for PlanBudget feature.
/// Handles data fetching, authentication state, category allocation management.
/// </summary>
public partial class PlanBudgetModel : ObservableObject
{
    private readonly CategoryManagementService _categoryService;
    private readonly CategoryAllocationManagementService _allocationService;
    private readonly TransactionManagementService _transactionService;
    private readonly IAuthenticationService _authService;

    // Available categories (left column) - categories not yet allocated
    public ObservableCollection<Category> AvailableCategories { get; } = new();
    
    // Budget allocations (center column) - allocated categories with budget amounts
    public ObservableCollection<CategoryAllocation> BudgetAllocations { get; } = new();
    
    // Track IDs of allocated categories to filter them from available list
    private readonly HashSet<int> _allocatedCategoryIds = new();
    
    // Track spent amounts per allocation ID
    private readonly Dictionary<int, decimal> _spentAmounts = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private string statusMessage = "Loading...";

    [ObservableProperty]
    private bool isCategoriesVisible = false;

    [ObservableProperty]
    private int currentMonth;

    [ObservableProperty]
    private int currentYear;
    
    [ObservableProperty]
    private decimal monthlyLimit = 0m;
    
    [ObservableProperty]
    private bool isEditMode = false;
    
    // Backup state for undo functionality
    private decimal _backupMonthlyLimit;
    private List<(int AllocationId, decimal BudgetedAmount)> _backupAllocations = new();

    public PlanBudgetModel(
        CategoryManagementService categoryService,
        CategoryAllocationManagementService allocationService,
        TransactionManagementService transactionService,
        IAuthenticationService authService)
    {
        _categoryService = categoryService;
        _allocationService = allocationService;
        _transactionService = transactionService;
        _authService = authService;
        
        // Default to current month/year
        var now = DateTime.Now;
        CurrentMonth = now.Month;
        CurrentYear = now.Year;
    }

    /// <summary>
    /// Initialize the model by checking user session and loading data.
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
            if (IsLoggedIn)
            {
                var userName = await _authService.GetUserNameAsync();
                StatusMessage = $"Logged in as {userName ?? "user"}";
            }
            else
            {
                IsLoggedIn = false;
                StatusMessage = "Please log in to plan budget";
                AvailableCategories.Clear();
                BudgetAllocations.Clear();
            }
        }
        catch
        {
            IsLoggedIn = false;
            StatusMessage = "Error checking login status";
            AvailableCategories.Clear();
            BudgetAllocations.Clear();
        }
    }

    /// <summary>
    /// Load all data: categories and existing allocations for current month/year.
    /// </summary>
    public async Task LoadDataAsync()
    {
        if (IsBusy || !IsLoggedIn) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Loading categories and allocations...";
            
            // Load existing allocations first to know which categories are already allocated
            await LoadExistingAllocationsAsync(CurrentMonth, CurrentYear);
            
            // Then load available categories (excluding already allocated ones)
            await LoadCategoriesAsync();

            StatusMessage = $"Budget plan for {GetMonthName(CurrentMonth)} {CurrentYear}";
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
    /// Load categories for the current user, filtering out already allocated ones.
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        AvailableCategories.Clear();

        var items = await _categoryService.GetCategoriesForUserAsync();
        
        // Only add categories that haven't been allocated yet
        foreach (var category in items)
        {
            if (!_allocatedCategoryIds.Contains(category.Id))
            {
                AvailableCategories.Add(category);
            }
        }
    }

    /// <summary>
    /// Load existing allocations for the specified month/year.
    /// </summary>
    private async Task LoadExistingAllocationsAsync(int month, int year)
    {
        BudgetAllocations.Clear();
        _allocatedCategoryIds.Clear();
        _spentAmounts.Clear();

        // Get all categories first to find their allocations
        var categories = await _categoryService.GetCategoriesForUserAsync();
        
        foreach (var category in categories)
        {
            var allocations = await _allocationService.GetAllocationsForCategoryAsync(category.Id);
            var allocation = allocations.FirstOrDefault(a => 
                a.Month == month && 
                a.Year == year && 
                a.IsActive);
            
            if (allocation != null)
            {
                // Ensure the Category navigation property is populated
                allocation.Category = category;
                BudgetAllocations.Add(allocation);
                _allocatedCategoryIds.Add(category.Id);
                
                // Load spent amount for this allocation
                await LoadSpentAmountAsync(allocation.Id);
            }
        }
    }
    
    /// <summary>
    /// Load the spent amount for a specific allocation from transaction splits.
    /// </summary>
    private async Task LoadSpentAmountAsync(int allocationId)
    {
        try
        {
            // Get the allocation to find its category
            var allocation = BudgetAllocations.FirstOrDefault(a => a.Id == allocationId);
            if (allocation == null) return;
            
            // Get all transaction splits for this category
            var splits = await _transactionService.GetTransactionSplitsForCategoryAsync(allocation.CategoryId);
            
            // Filter splits that belong to this specific allocation (by matching the allocation ID)
            var relevantSplits = splits.Where(s => s.CategoryAllocationId == allocationId);
            
            // Sum the amounts (expenses are positive, income would be negative if IsIncome = true)
            var spent = relevantSplits.Sum(s => s.IsIncome ? -s.Amount : s.Amount);
            
            _spentAmounts[allocationId] = spent;
        }
        catch (Exception)
        {
            // If we can't load spent amount, default to 0
            _spentAmounts[allocationId] = 0;
        }
    }
    
    /// <summary>
    /// Get the spent amount for a specific allocation.
    /// </summary>
    public decimal GetSpentAmount(int allocationId)
    {
        return _spentAmounts.TryGetValue(allocationId, out var amount) ? amount : 0;
    }
    
    /// <summary>
    /// Get the remaining amount for a specific allocation.
    /// </summary>
    public decimal GetRemainingAmount(CategoryAllocation allocation)
    {
        var spent = GetSpentAmount(allocation.Id);
        return allocation.BudgetedAmount - spent;
    }
    
    /// <summary>
    /// Calculate total allocated amount across all budget allocations.
    /// </summary>
    public decimal GetTotalAllocated()
    {
        return BudgetAllocations.Sum(a => a.BudgetedAmount);
    }
    
    /// <summary>
    /// Calculate unallocated amount (monthly limit minus allocated).
    /// </summary>
    public decimal GetUnallocated()
    {
        return MonthlyLimit - GetTotalAllocated();
    }
    
    /// <summary>
    /// Enter edit mode and backup current state for undo.
    /// </summary>
    public void EnterEditMode()
    {
        IsEditMode = true;
        _backupMonthlyLimit = MonthlyLimit;
        _backupAllocations = BudgetAllocations
            .Select(a => (a.Id, a.BudgetedAmount))
            .ToList();
    }
    
    /// <summary>
    /// Exit edit mode without saving (undo changes).
    /// </summary>
    public void UndoChanges()
    {
        MonthlyLimit = _backupMonthlyLimit;
        
        foreach (var backup in _backupAllocations)
        {
            var allocation = BudgetAllocations.FirstOrDefault(a => a.Id == backup.AllocationId);
            if (allocation != null)
            {
                allocation.BudgetedAmount = backup.BudgetedAmount;
            }
        }
        
        IsEditMode = false;
    }
    
    /// <summary>
    /// Exit edit mode (user clicked Cancel without undo).
    /// </summary>
    public void CancelEdit()
    {
        UndoChanges(); // Cancel is same as undo
    }

    /// <summary>
    /// Refresh data by checking session and reloading.
    /// </summary>
    public async Task RefreshAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadDataAsync();
        }
    }

    /// <summary>
    /// Toggle categories list visibility.
    /// </summary>
    public void ToggleCategoriesVisibility()
    {
        IsCategoriesVisible = !IsCategoriesVisible;
    }

    /// <summary>
    /// Allocate a category to the budget plan with initial amount of 0.
    /// Moves category from available list to allocations list.
    /// </summary>
    public void AllocateCategory(Category category)
    {
        if (category == null || !AvailableCategories.Contains(category))
            return;

        // Create new allocation with amount = 0
        var allocation = new CategoryAllocation
        {
            CategoryId = category.Id,
            Category = category,
            BudgetedAmount = 0,
            Month = CurrentMonth,
            Year = CurrentYear,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Move from available to allocated
        AvailableCategories.Remove(category);
        BudgetAllocations.Add(allocation);
        _allocatedCategoryIds.Add(category.Id);
    }

    /// <summary>
    /// Remove an allocation from the budget plan.
    /// Moves category back to available list if it hasn't been persisted yet.
    /// </summary>
    public void RemoveAllocation(CategoryAllocation allocation)
    {
        if (allocation == null || !BudgetAllocations.Contains(allocation))
            return;

        BudgetAllocations.Remove(allocation);
        _allocatedCategoryIds.Remove(allocation.CategoryId);

        // Only add back to available if the category object exists
        if (allocation.Category != null)
        {
            AvailableCategories.Add(allocation.Category);
        }
    }

    /// <summary>
    /// Handle a newly created category by adding it to the allocations list.
    /// </summary>
    public async Task HandleNewCategoryAsync()
    {
        try
        {
            // Get all categories to find the new one
            var allCategories = await _categoryService.GetCategoriesForUserAsync();
            
            // Find categories that aren't in available or allocated lists
            var existingIds = new HashSet<int>(AvailableCategories.Select(c => c.Id));
            existingIds.UnionWith(_allocatedCategoryIds);
            
            var newCategory = allCategories.FirstOrDefault(c => !existingIds.Contains(c.Id));
            
            if (newCategory != null)
            {
                // Automatically allocate the new category
                AllocateCategory(newCategory);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding category: {ex.Message}";
        }
    }

    /// <summary>
    /// Save all allocations to the API.
    /// Creates new allocations or updates existing ones.
    /// </summary>
    public async Task SaveAllocationsAsync()
    {
        if (IsBusy || !IsLoggedIn) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Saving budget allocations...";

            int savedCount = 0;
            foreach (var allocation in BudgetAllocations)
            {
                // Only save allocations that don't have an Id yet (new ones)
                if (allocation.Id == 0)
                {
                    var record = new CategoryAllocationRecord(
                        CategoryId: allocation.CategoryId,
                        BudgetedAmount: allocation.BudgetedAmount,
                        Month: allocation.Month,
                        Year: allocation.Year,
                        EditorName: allocation.EditorName,
                        PercentageAllocation: allocation.PercentageAllocation,
                        OldAmount: allocation.OldAmount,
                        EditedMemo: allocation.EditedMemo
                    );

                    var newId = await _allocationService.CreateCategoryAllocationAsync(record);
                    allocation.Id = newId;
                    savedCount++;
                }
                // TODO: Handle updates for existing allocations when update API exists
            }

            StatusMessage = savedCount > 0 
                ? $"Saved {savedCount} allocation(s)" 
                : "No new allocations to save";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving allocations: {ex.Message}";
            throw; // Re-throw so ViewModel can handle error UI
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Set the target month/year and reload allocations.
    /// </summary>
    public async Task SetMonthYearAsync(int month, int year)
    {
        if (month < 1 || month > 12 || year < 2000 || year > 2100)
            return;

        CurrentMonth = month;
        CurrentYear = year;

        if (IsLoggedIn)
        {
            await LoadDataAsync();
        }
    }

    /// <summary>
    /// Helper method to get month name from number.
    /// </summary>
    private static string GetMonthName(int month)
    {
        return month switch
        {
            1 => "January",
            2 => "February",
            3 => "March",
            4 => "April",
            5 => "May",
            6 => "June",
            7 => "July",
            8 => "August",
            9 => "September",
            10 => "October",
            11 => "November",
            12 => "December",
            _ => "Unknown"
        };
    }
}
