using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Data;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for PlanBudgetPage - thin coordination layer between View and Model.
/// Handles UI-specific concerns like navigation and popups, delegates business logic to Model.
/// </summary>
public partial class PlanBudgetViewModel : ObservableObject
{
    private readonly IMVMPopupService _popupService;
    private readonly IAuthenticationService _authenticationService;

    public PlanBudgetModel Model { get; }

    // Month names for picker
    public List<string> MonthOptions { get; } = new()
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

    // Month abbreviations for grid display
    public List<string> MonthAbbreviations { get; } = new()
    {
        "Jan", "Feb", "Mar", "Apr", "May", "Jun",
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    };

    // Selected month name for picker binding
    public string SelectedMonthName
    {
        get => MonthOptions[Model.CurrentMonth - 1];
        set
        {
            var index = MonthOptions.IndexOf(value);
            if (index >= 0)
            {
                Model.CurrentMonth = index + 1;
                OnPropertyChanged();
            }
        }
    }
    
    /// <summary>
    /// Gets the month abbreviation for the current month.
    /// </summary>
    public string CurrentMonthAbbreviation => MonthAbbreviations[Model.CurrentMonth - 1];
    
    /// <summary>
    /// Gets or sets whether the month/year picker is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isMonthYearPickerVisible;
    
    /// <summary>
    /// Toggle the month/year picker visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleMonthYearPicker()
    {
        if (!Model.IsEditMode)
        {
            IsMonthYearPickerVisible = !IsMonthYearPickerVisible;
        }
    }
    
    /// <summary>
    /// Close the month/year picker.
    /// </summary>
    [RelayCommand]
    private void CloseMonthYearPicker()
    {
        IsMonthYearPickerVisible = false;
    }
    
    /// <summary>
    /// Select a specific month by index (1-12) and auto-load budget.
    /// </summary>
    [RelayCommand]
    private async Task SelectMonth(object? parameter)
    {
        // Handle both int and string parameters from XAML
        int month = 0;
        if (parameter is int intMonth)
        {
            month = intMonth;
        }
        else if (parameter is string strMonth && int.TryParse(strMonth, out int parsedMonth))
        {
            month = parsedMonth;
        }
        
        if (month >= 1 && month <= 12 && Model.CurrentMonth != month)
        {
            Model.CurrentMonth = month;
            OnPropertyChanged(nameof(SelectedMonthName));
            OnPropertyChanged(nameof(CurrentMonthAbbreviation));
            await Model.SetMonthYearAsync(Model.CurrentMonth, Model.CurrentYear);
        }
    }
    
    /// <summary>
    /// Select a month from the picker and close it.
    /// </summary>
    [RelayCommand]
    private async Task SelectMonthFromPicker(object? parameter)
    {
        await SelectMonth(parameter);
        IsMonthYearPickerVisible = false;
    }
    
    /// <summary>
    /// Increment the current year and auto-load budget.
    /// </summary>
    [RelayCommand]
    private async Task NextYear()
    {
        if (Model.CurrentYear < 2100)
        {
            Model.CurrentYear++;
            await Model.SetMonthYearAsync(Model.CurrentMonth, Model.CurrentYear);
        }
    }
    
    /// <summary>
    /// Decrement the current year and auto-load budget.
    /// </summary>
    [RelayCommand]
    private async Task PreviousYear()
    {
        if (Model.CurrentYear > 2000)
        {
            Model.CurrentYear--;
            await Model.SetMonthYearAsync(Model.CurrentMonth, Model.CurrentYear);
        }
    }
    
    /// <summary>
    /// Navigate to the previous month and auto-load budget.
    /// </summary>
    [RelayCommand]
    private async Task PreviousMonth()
    {
        var newMonth = Model.CurrentMonth - 1;
        var newYear = Model.CurrentYear;
        
        if (newMonth < 1)
        {
            newMonth = 12;
            newYear--;
        }
        
        if (newYear >= 2000)
        {
            Model.CurrentMonth = newMonth;
            Model.CurrentYear = newYear;
            OnPropertyChanged(nameof(SelectedMonthName));
            OnPropertyChanged(nameof(CurrentMonthAbbreviation));
            await Model.SetMonthYearAsync(Model.CurrentMonth, Model.CurrentYear);
        }
    }
    
    /// <summary>
    /// Navigate to the next month and auto-load budget.
    /// </summary>
    [RelayCommand]
    private async Task NextMonth()
    {
        var newMonth = Model.CurrentMonth + 1;
        var newYear = Model.CurrentYear;
        
        if (newMonth > 12)
        {
            newMonth = 1;
            newYear++;
        }
        
        if (newYear <= 2100)
        {
            Model.CurrentMonth = newMonth;
            Model.CurrentYear = newYear;
            OnPropertyChanged(nameof(SelectedMonthName));
            OnPropertyChanged(nameof(CurrentMonthAbbreviation));
            await Model.SetMonthYearAsync(Model.CurrentMonth, Model.CurrentYear);
        }
    }

    public PlanBudgetViewModel(PlanBudgetModel model, IMVMPopupService popupService, IAuthenticationService authenticationService)
    {
        Model = model;
        _popupService = popupService;
        _authenticationService = authenticationService;
        
        // Subscribe to model changes to update computed properties
        Model.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Model.BudgetAllocations))
            {
                OnPropertyChanged(nameof(TotalAllocatedFormatted));
                OnPropertyChanged(nameof(HasNoBudgetAllocations));
            }

            if (e.PropertyName == nameof(Model.ReadyToAssign))
            {
                OnPropertyChanged(nameof(ReadyToAssignFormatted));
            }

            if (e.PropertyName == nameof(Model.InactiveAllocations))
            {
                OnPropertyChanged(nameof(HasInactiveAllocations));
            }
        };
    }

    /// <summary>
    /// Formatted total allocated amount for MAUI binding.
    /// </summary>
    public string TotalAllocatedFormatted => Model.GetTotalAllocated().ToString("C");

    /// <summary>
    /// Formatted Ready To Assign amount for MAUI binding.
    /// </summary>
    public string ReadyToAssignFormatted => Model.ReadyToAssign.ToString("C");

    /// <summary>
    /// Check if there are no budget allocations for empty state display.
    /// </summary>
    public bool HasNoBudgetAllocations => Model.BudgetAllocations == null || Model.BudgetAllocations.Count == 0;
    
    /// <summary>
    /// Check if there are inactive allocations to show the dropdown.
    /// </summary>
    public bool HasInactiveAllocations => Model.InactiveAllocations != null && Model.InactiveAllocations.Count > 0;
    
    /// <summary>
    /// Gets or sets whether the inactive categories section is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isInactiveCategoriesExpanded;

    /// <summary>
    /// Initialize the ViewModel by delegating to the Model.
    /// </summary>
    public async Task InitializeAsync()
    {
        await Model.InitializeAsync();
        
    }

    /// <summary>
    /// Refresh command - delegates to Model.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await Model.RefreshAsync();
    }

    /// <summary>
    /// Navigate to Home command - pure navigation logic.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToHome()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    /// <summary>
    /// Cancel command with confirmation dialog.
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        bool confirmed = await _popupService.DisplayAlertAsync(
            "Cancel Confirmation",
            "Are you sure you want to cancel? Any unsaved changes will be lost.",
            "Yes",
            "No");
            
        if (confirmed)
        {
            await NavigateToHome();
        }
    }

    /// <summary>
    /// Save command - shows confirmation then delegates to Model.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await Model.SaveAllocationsAsync();
            
            // Show success message
            await _popupService.DisplayAlertAsync(
                "Success",
                Model.StatusMessage);
        }
        catch (Exception ex)
        {
            // Show error message
            await _popupService.DisplayAlertAsync(
                "Error",
                $"Failed to save allocations: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggle categories list visibility - delegates to Model.
    /// </summary>
    [RelayCommand]
    private void ToggleCategoriesVisibility()
    {
        Model.ToggleCategoriesVisibility();
    }

    /// <summary>
    /// Add Category command - shows popup then refreshes to include new category.
    /// Pure UI coordination - shows popup and triggers Model to handle new category.
    /// </summary>
    [RelayCommand]
    private async Task AddCategory()
    {
        await _popupService.ShowAddCategoryAsync();
        await Model.HandleNewCategoryAsync();
    }

    /// <summary>
    /// Allocate category command - delegates to Model.
    /// Moves category from available list to budget allocations.
    /// </summary>
    [RelayCommand]
    private void AllocateCategory(Category category)
    {
        Model.AllocateCategory(category);
    }

    /// <summary>
    /// Remove allocation command - delegates to Model.
    /// Removes allocation from budget and returns category to available list.
    /// </summary>
    [RelayCommand]
    private void RemoveAllocation(CategoryAllocation allocation)
    {
        Model.RemoveAllocation(allocation);
    }

    /// <summary>
    /// Set month/year command - delegates to Model to load allocations for that period.
    /// </summary>
    [RelayCommand]
    private async Task SetMonthYear()
    {
        await Model.SetMonthYearAsync(Model.CurrentMonth, Model.CurrentYear);
    }
    
    /// <summary>
    /// Toggle edit mode - enter or exit edit mode.
    /// </summary>
    [RelayCommand]
    private void ToggleEditMode()
    {
        if (Model.IsEditMode)
        {
            // Exiting edit mode - this would be the "Save Changes" action
            // Don't actually save here, let SaveAsync handle that
            Model.IsEditMode = false;
        }
        else
        {
            // Entering edit mode
            Model.EnterEditMode();
        }
    }
    
    /// <summary>
    /// Undo changes and exit edit mode without saving.
    /// </summary>
    [RelayCommand]
    private void UndoChanges()
    {
        Model.UndoChanges();
    }
    
    /// <summary>
    /// Cancel edit mode (same as undo for now).
    /// </summary>
    [RelayCommand]
    private void CancelEditMode()
    {
        Model.CancelEdit();
    }
    
    /// <summary>
    /// Login command - shows login popup and refreshes authentication state on success.
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        var success = await _authenticationService.LoginAsync();
        if (success)
        {
            // Refresh user session after successful login
            await Model.CheckUserSessionAsync();
        }
        else
        {
            await _popupService.DisplayAlertAsync("Login Failed", "Unable to authenticate. Please try again.");
        }
    }
    
    /// <summary>
    /// Deactivate allocation command - delegates to Model.
    /// Moves allocation from active to inactive section.
    /// </summary>
    [RelayCommand]
    private async Task DeactivateAllocation(CategoryAllocation allocation)
    {
        try
        {
            await Model.DeactivateAllocationAsync(allocation);
            OnPropertyChanged(nameof(HasNoBudgetAllocations));
            OnPropertyChanged(nameof(HasInactiveAllocations));
        }
        catch (Exception ex)
        {
            await _popupService.DisplayAlertAsync("Error", $"Failed to deactivate category: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Activate allocation command - delegates to Model.
    /// Moves allocation from inactive to active section.
    /// </summary>
    [RelayCommand]
    private async Task ActivateAllocation(CategoryAllocation allocation)
    {
        try
        {
            await Model.ActivateAllocationAsync(allocation);
            OnPropertyChanged(nameof(HasNoBudgetAllocations));
            OnPropertyChanged(nameof(HasInactiveAllocations));
        }
        catch (Exception ex)
        {
            await _popupService.DisplayAlertAsync("Error", $"Failed to activate category: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Toggle the inactive categories section visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleInactiveCategories()
    {
        IsInactiveCategoriesExpanded = !IsInactiveCategoriesExpanded;
    }
    
    /// <summary>
    /// Get formatted activity (spent) amount for an allocation from snapshot data.
    /// </summary>
    public string GetActivityFormatted(CategoryAllocation allocation)
    {
        if (Model.CurrentSnapshot == null)
            return "$0.00";
            
        var categorySnapshot = Model.CurrentSnapshot.Categories
            .FirstOrDefault(c => c.CategoryId == allocation.CategoryId);
            
        return categorySnapshot?.Activity.ToString("C") ?? "$0.00";
    }
    
    /// <summary>
    /// Get formatted available (remaining) amount for an allocation from snapshot data.
    /// </summary>
    public string GetAvailableFormatted(CategoryAllocation allocation)
    {
        if (Model.CurrentSnapshot == null)
            return "$0.00";
            
        var categorySnapshot = Model.CurrentSnapshot.Categories
            .FirstOrDefault(c => c.CategoryId == allocation.CategoryId);
            
        return categorySnapshot?.Available.ToString("C") ?? "$0.00";
    }
    
    /// <summary>
    /// Get progress percentage (0-1) for an allocation from snapshot data.
    /// </summary>
    public double GetProgressPercentage(CategoryAllocation allocation)
    {
        if (allocation.BudgetedAmount <= 0 || Model.CurrentSnapshot == null)
            return 0;
            
        var categorySnapshot = Model.CurrentSnapshot.Categories
            .FirstOrDefault(c => c.CategoryId == allocation.CategoryId);
            
        if (categorySnapshot == null)
            return 0;
            
        var percentage = (double)Math.Abs(categorySnapshot.Activity) / (double)allocation.BudgetedAmount;
        return Math.Min(percentage, 1.0); // Cap at 100%
    }
    
    /// <summary>
    /// Check if allocation is overspent using snapshot data.
    /// </summary>
    public bool IsOverspent(CategoryAllocation allocation)
    {
        if (Model.CurrentSnapshot == null)
            return false;
            
        var categorySnapshot = Model.CurrentSnapshot.Categories
            .FirstOrDefault(c => c.CategoryId == allocation.CategoryId);
            
        return categorySnapshot?.Available < 0;
    }
    
    /// <summary>
    /// Get color for available amount (green if positive, red if negative) from snapshot data.
    /// </summary>
    public Color GetAvailableColor(CategoryAllocation allocation)
    {
        if (Model.CurrentSnapshot == null)
            return Colors.Gray;
            
        var categorySnapshot = Model.CurrentSnapshot.Categories
            .FirstOrDefault(c => c.CategoryId == allocation.CategoryId);
            
        if (categorySnapshot == null)
            return Colors.Gray;
            
        return categorySnapshot.Available >= 0 ? Colors.Green : Colors.Red;
    }
}
