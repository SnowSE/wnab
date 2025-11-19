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
    private readonly IBudgetSnapshotService _budgetSnapshotService;
    private readonly IBudgetService _budgetService;

    // Available categories - categories not yet allocated
    public ObservableCollection<Category> AvailableCategories { get; } = new();

    // Budget allocations - allocated categories with budget amounts (active only)
    public ObservableCollection<CategoryAllocation> BudgetAllocations { get; } = new();
    
    // Hidden allocations - allocated categories that are hidden (IsActive = false)
    public ObservableCollection<CategoryAllocation> HiddenAllocations { get; } = new();
    
    // Changed allocations - changed allocated categories with budget amounts
    public ObservableCollection<CategoryAllocation> ChangedAllocations { get; } = new();
    
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
    private decimal readyToAssign = 0m;

    [ObservableProperty]
    private bool isEditMode = false;
    
    // Current budget snapshot for the displayed month/year
    [ObservableProperty]
    private BudgetSnapshot? currentSnapshot;
    
    // Backup state for undo functionality
    private List<(int CategoryId, decimal BudgetedAmount)> _backupAllocations = new();

    public PlanBudgetModel(
        CategoryManagementService categoryService,
        CategoryAllocationManagementService allocationService,
        TransactionManagementService transactionService,
        IAuthenticationService authService,
        IBudgetSnapshotService budgetSnapshotService,
        IBudgetService budgetService)
    {
        _categoryService = categoryService;
        _allocationService = allocationService;
        _transactionService = transactionService;
        _authService = authService;
        _budgetSnapshotService = budgetSnapshotService;
        _budgetService = budgetService;

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
                StatusMessage = "Error checking login status";
                AvailableCategories.Clear();
                BudgetAllocations.Clear();
                HiddenAllocations.Clear();
                OnPropertyChanged(nameof(BudgetAllocations));
                OnPropertyChanged(nameof(HiddenAllocations));
            }
        }
        catch
        {
            IsLoggedIn = false;
            StatusMessage = "Error checking login status";
            AvailableCategories.Clear();
            BudgetAllocations.Clear();
            HiddenAllocations.Clear();
            OnPropertyChanged(nameof(BudgetAllocations));
            OnPropertyChanged(nameof(HiddenAllocations));
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

            await CalculateReadyToAssignAsync(CurrentMonth, CurrentYear);

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
    /// Creates allocations for ALL categories - both those with existing data and new ones.
    /// New categories (Id=0) are automatically hidden until user shows them.
    /// </summary>
    private async Task LoadExistingAllocationsAsync(int month, int year)
    {
        BudgetAllocations.Clear();
        HiddenAllocations.Clear();
        _allocatedCategoryIds.Clear();
        _spentAmounts.Clear();

        // Get all categories first
        var categories = await _categoryService.GetCategoriesForUserAsync();
        
        // Get the budget snapshot for this month and store it
        var snapshot = await _budgetSnapshotService.GetSnapshotAsync(month, year);
        CurrentSnapshot = snapshot;
        
        // Determine prior month/year
        var priorMonth = month - 1;
        var priorYear = year;
        
        if (priorMonth < 1)
        {
            priorMonth = 12;
            priorYear--;
        }
        
        foreach (var category in categories)
        {
            var allocations = await _allocationService.GetAllocationsForCategoryAsync(category.Id);
            var allocation = allocations.FirstOrDefault(a =>
                a.Month == month &&
                a.Year == year);

            var pastMonthAllocation = allocations.FirstOrDefault(a =>
                a.Month == priorMonth &&
                a.Year == priorYear);

                
            
            // If no allocation exists for this category/month, create a temporary one
            if (allocation == null)
            {
                allocation = new CategoryAllocation
                {
                    Id = 0, // Not yet persisted
                    CategoryId = category.Id,
                    Category = category,
                    BudgetedAmount = 0,
                    Month = month,
                    Year = year,
                    IsActive = pastMonthAllocation?.IsActive ?? category.IsActive, // New allocations follow past month status
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };


                // New allocations always go to hidden
                if (!allocation.IsActive)
                {
                    HiddenAllocations.Add(allocation);
                }
                else
                {
                    BudgetAllocations.Add(allocation);
                }
            }
            else
            {
                // Ensure the Category navigation property is populated
                allocation.Category = category;
                
                // Existing allocations go to appropriate collection based on IsActive
                if (allocation.IsActive)
                {
                    BudgetAllocations.Add(allocation);
                }
                else
                {
                    HiddenAllocations.Add(allocation);
                }
                
                // Load spent amount for existing allocations
                await LoadSpentAmountAsync(allocation.Id);
            }
            
            // TODO: Map snapshot data to allocation for display
            // Snapshot data will be accessed separately for UI display
            
            _allocatedCategoryIds.Add(category.Id);
        }

        // Calculate Ready To Assign for the current month
        await CalculateReadyToAssignAsync(month, year);

        // Notify UI that collections have changed
        OnPropertyChanged(nameof(BudgetAllocations));
        OnPropertyChanged(nameof(HiddenAllocations));
    }

    /// <summary>
    /// Calculate the Ready To Assign value for the specified month/year.
    /// </summary>
    public async Task CalculateReadyToAssignAsync(int month, int year)
    {
        try
        {
            // Calculate RTA using the budget service
            ReadyToAssign = await _budgetService.CalculateReadyToAssign(month, year);
        }
        catch (Exception ex)
        {
            // Log the actual error for debugging
            System.Diagnostics.Debug.WriteLine($"Failed to calculate RTA: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw new InvalidOperationException($"Failed to calculate Ready To Assign: {ex.Message}", ex);
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
            var splits = await _transactionService.GetTransactionSplitsForAllocationAsync(allocation.CategoryId);
            
            // Filter splits that belong to this specific allocation (by matching the allocation ID)
            var relevantSplits = splits.Where(s => s.CategoryAllocationId == allocationId);
            
            // Sum the amounts (expenses are positive, income would be negative if IsIncome = true)
            var spent = relevantSplits.Sum(s => s.Amount);
            
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
    /// Enter edit mode and backup current state for undo.
    /// </summary>
    public void EnterEditMode()
    {
        IsEditMode = true;
        _backupAllocations = BudgetAllocations
            .Select(a => (a.CategoryId, a.BudgetedAmount))
            .ToList();
    }
    
    /// <summary>
    /// Exit edit mode without saving (undo changes).
    /// </summary>
    public void UndoChanges()
    {
        foreach (var backup in _backupAllocations)
        {
            var allocation = BudgetAllocations.FirstOrDefault(a => a.CategoryId == backup.CategoryId);
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
        
        // Notify UI that collection has changed
        OnPropertyChanged(nameof(BudgetAllocations));
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
        
        // Notify UI that collection has changed
        OnPropertyChanged(nameof(BudgetAllocations));
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
            int updatedCount = 0;
            
            foreach (var allocation in BudgetAllocations)
            {
                // New allocation (not yet persisted)
                if (allocation.Id == 0)
                {
                    // Only create allocation if it has a non-zero amount
                    if (allocation.BudgetedAmount > 0)
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
                }
                // Existing allocation - check if amount changed
                else
                {
                    var backup = _backupAllocations.FirstOrDefault(b => b.CategoryId == allocation.CategoryId);
                    if (backup != default && backup.BudgetedAmount != allocation.BudgetedAmount)
                    {
                        var updateRequest = new UpdateCategoryAllocationRequest(
                            Id: allocation.Id,
                            BudgetedAmount: allocation.BudgetedAmount,
                            EditorName: await _authService.GetUserNameAsync()
                        );

                        await _allocationService.UpdateCategoryAllocationAsync(updateRequest);
                        updatedCount++;
                    }
                }
            }

            var message = new List<string>();
            if (savedCount > 0) message.Add($"Created {savedCount} allocation(s)");
            if (updatedCount > 0) message.Add($"Updated {updatedCount} allocation(s)");
            
            StatusMessage = message.Count > 0 
                ? string.Join(", ", message)
                : "No changes to save";
            
            // Invalidate snapshots from current month forward since allocations changed
            if (savedCount > 0 || updatedCount > 0)
            {
                await _budgetSnapshotService.InvalidateSnapshotsFromMonthAsync(CurrentMonth, CurrentYear);
            }
                
            // Exit edit mode after successful save
            IsEditMode = false;
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
    /// Hide a category allocation (set IsActive to false).
    /// Moves allocation from BudgetAllocations to HiddenAllocations.
    /// For new allocations (Id=0), just updates in memory without API call.
    /// </summary>
    public async Task HideAllocationAsync(CategoryAllocation allocation)
    {
        if (allocation == null)
            return;

        try
        {
            // For existing allocations, update via API
            if (allocation.Id > 0)
            {
                var request = new UpdateCategoryAllocationRequest(
                    Id: allocation.Id,
                    IsActive: false
                );
                
                await _allocationService.UpdateCategoryAllocationAsync(request);
            }
            
            // Update local state (for both new and existing allocations)
            allocation.IsActive = false;
            BudgetAllocations.Remove(allocation);
            HiddenAllocations.Add(allocation);
            
            // Notify UI that collections have changed
            OnPropertyChanged(nameof(BudgetAllocations));
            OnPropertyChanged(nameof(HiddenAllocations));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error hiding category: {ex.Message}";
            throw;
        }
    }

    /// <summary>
    /// Unhide a category allocation (set IsActive to true).
    /// Moves allocation from HiddenAllocations to BudgetAllocations.
    /// For new allocations (Id=0), just updates in memory without API call.
    /// </summary>
    public async Task UnhideAllocationAsync(CategoryAllocation allocation)
    {
        if (allocation == null)
            return;

        try
        {
            // For existing allocations, update via API
            if (allocation.Id > 0)
            {
                var request = new UpdateCategoryAllocationRequest(
                    Id: allocation.Id,
                    IsActive: true
                );
                
                await _allocationService.UpdateCategoryAllocationAsync(request);
            }
            
            // Update local state (for both new and existing allocations)
            allocation.IsActive = true;
            HiddenAllocations.Remove(allocation);
            BudgetAllocations.Add(allocation);
            
            // Notify UI that collections have changed
            OnPropertyChanged(nameof(BudgetAllocations));
            OnPropertyChanged(nameof(HiddenAllocations));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error unhiding category: {ex.Message}";
            throw;
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
