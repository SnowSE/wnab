using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Data;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for CategoriesPage - thin coordination layer between View and Model.
/// Handles UI-specific concerns like navigation and popups, delegates business logic to Model.
/// </summary>
public partial class CategoriesViewModel : ObservableObject
{
    private readonly IMVMPopupService _popupService;
    private readonly CategoryManagementService _categoryService;

    public CategoriesModel Model { get; }

    // Popup visibility properties
    [ObservableProperty]
    private bool canSeeAddPopup;

    [ObservableProperty]
    private bool canSeeEditPopup;

    public CategoriesViewModel(CategoriesModel model, IMVMPopupService popupService, CategoryManagementService categoryService)
    {
        Model = model;
        _popupService = popupService;
        _categoryService = categoryService;
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
    /// Edit Category command - shows edit popup then refreshes the list.
    /// </summary>
    [RelayCommand]
    private async Task EditCategory(Category category)
    {
        if (category == null) return;

        await _popupService.ShowEditCategoryAsync(category.Id, category.Name, category.Color, category.IsActive);
        await Model.RefreshAsync();
    }

    /// <summary>
    /// Delete Category command - confirms deletion then calls API.
    /// </summary>
    [RelayCommand]
    private async Task DeleteCategory(Category category)
    {
        if (category == null) return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Delete Category",
            $"Are you sure you want to delete '{category.Name}'? This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            await _categoryService.DeleteCategoryAsync(category.Id);
            await Model.RefreshAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to delete category: {ex.Message}", "OK");
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
