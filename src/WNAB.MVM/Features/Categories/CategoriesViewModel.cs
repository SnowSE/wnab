using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for CategoriesPage - thin coordination layer between View and Model.
/// Handles UI-specific concerns like navigation, delegates business logic to Model.
/// Uses inline editing instead of popups.
/// </summary>
public partial class CategoriesViewModel : ObservableObject
{
    private readonly CategoryManagementService _categoryService;
    private readonly IAlertService _alertService;
    private readonly AddCategoryModel _addCategoryModel;

    public CategoriesModel Model { get; }
    
    /// <summary>
    /// Model for inline add category form.
    /// </summary>
    public AddCategoryModel AddCategoryModel => _addCategoryModel;

    /// <summary>
    /// Controls visibility of inline add category form.
    /// </summary>
    [ObservableProperty]
    private bool _isAddFormVisible;

    public CategoriesViewModel(CategoriesModel model, CategoryManagementService categoryService, IAlertService alertService, AddCategoryModel addCategoryModel)
    {
        Model = model;
        _categoryService = categoryService;
        _alertService = alertService;
        _addCategoryModel = addCategoryModel;
        _isAddFormVisible = false;
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
    /// Toggle add category form visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleAddForm()
    {
        _isAddFormVisible = !_isAddFormVisible;
        if (!_isAddFormVisible)
        {
            _addCategoryModel.Reset();
        }
    }

    /// <summary>
    /// Cancel add category - hides form and resets.
    /// </summary>
    [RelayCommand]
    private void CancelAddCategory()
    {
        _isAddFormVisible = false;
        _addCategoryModel.Reset();
    }

    /// <summary>
    /// Save new category from inline form.
    /// </summary>
    [RelayCommand]
    private async Task SaveNewCategory()
    {
        var success = await _addCategoryModel.CreateCategoryAsync();
        if (success)
        {
            _isAddFormVisible = false;
            _addCategoryModel.Reset();
            await Model.RefreshAsync();
        }
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
