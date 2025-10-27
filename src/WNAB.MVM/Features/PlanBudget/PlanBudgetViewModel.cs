using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Data;
using WNAB.Services;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for PlanBudgetPage - thin coordination layer between View and Model.
/// Handles UI-specific concerns like navigation and popups, delegates business logic to Model.
/// </summary>
public partial class PlanBudgetViewModel : ObservableObject
{
    private readonly IMVMPopupService _popupService;

    public PlanBudgetModel Model { get; }

    // Month names for picker
    public List<string> MonthOptions { get; } = new()
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
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

    public PlanBudgetViewModel(PlanBudgetModel model, IMVMPopupService popupService)
    {
        Model = model;
        _popupService = popupService;
    }

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
        bool confirmed = await Shell.Current.DisplayAlertAsync(
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
            await Shell.Current.DisplayAlertAsync(
                "Success",
                Model.StatusMessage,
                "OK");
        }
        catch (Exception ex)
        {
            // Show error message
            await Shell.Current.DisplayAlertAsync(
                "Error",
                $"Failed to save allocations: {ex.Message}",
                "OK");
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
}
