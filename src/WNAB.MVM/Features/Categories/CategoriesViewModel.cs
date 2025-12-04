using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for CategoriesPage - thin coordination layer between View and Model.
/// Handles UI-specific concerns like navigation and popups, delegates business logic to Model.
/// </summary>
public partial class CategoriesViewModel : ObservableObject
{
    private readonly IMVMPopupService _popupService;
    private readonly CategoryManagementService _categoryService;
    private readonly IAlertService _alertService;

    public CategoriesModel Model { get; }

    // Popup visibility properties
    [ObservableProperty]
    private bool canSeeAddPopup;

    [ObservableProperty]
    private bool canSeeEditPopup;

    public CategoriesViewModel(CategoriesModel model, IMVMPopupService popupService, CategoryManagementService categoryService, IAlertService alertService)
    {
        Model = model;
        _popupService = popupService;
        _categoryService = categoryService;
        _alertService = alertService;
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
    /// Add Category command - shows popup then refreshes the list.
    /// Pure UI coordination - shows popup and triggers refresh.
    /// </summary>
    [RelayCommand]
    private async Task AddCategory()
    {
        await _popupService.ShowAddCategoryAsync();
        await Model.RefreshAsync();
    }

    /// <summary>
    /// Start inline editing for a category item.
    /// </summary>
    [RelayCommand]
    private void EditCategoryInline(CategoryItemViewModel categoryItem)
    {
        if (categoryItem == null) return;
        categoryItem.StartEditing();
    }

    /// <summary>
    /// Save inline edits for a category item.
    /// </summary>
    [RelayCommand]
    private async Task SaveCategoryInline(CategoryItemViewModel categoryItem)
    {
        if (categoryItem == null) return;

        if (string.IsNullOrWhiteSpace(categoryItem.EditName))
        {
            await _alertService.DisplayAlertAsync("Error", "Category name cannot be empty.");
            return;
        }

        var (success, errorMessage) = await Model.UpdateCategoryAsync(
            categoryItem.Id, 
            categoryItem.EditName, 
            categoryItem.EditColor, 
            categoryItem.IsActive);

        if (success)
        {
            categoryItem.ApplyChanges();
        }
        else
        {
            await _alertService.DisplayAlertAsync("Error", errorMessage ?? "Failed to update category. Please try again.");
        }
    }

    /// <summary>
    /// Cancel inline editing for a category item.
    /// </summary>
    [RelayCommand]
    private void CancelEditCategoryInline(CategoryItemViewModel categoryItem)
    {
        if (categoryItem == null) return;
        categoryItem.CancelEditing();
    }

    /// <summary>
    /// Delete Category command - confirms deletion then calls API.
    /// </summary>
    [RelayCommand]
    private async Task DeleteCategory(CategoryItemViewModel categoryItem)
    {
        if (categoryItem == null) return;

        var confirmed = await _alertService.DisplayAlertAsync(
            "Delete Category",
            $"Are you sure you want to delete '{categoryItem.Name}'? This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            await _categoryService.DeleteCategoryAsync(categoryItem.Id);
            await Model.RefreshAsync();
        }
        catch (Exception ex)
        {
            await _alertService.DisplayAlertAsync("Error", $"Failed to delete category: {ex.Message}");
        }
    }

    /// <summary>
    /// Navigate to Home command - pure navigation logic.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToHome()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
