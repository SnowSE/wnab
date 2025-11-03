using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for editing a category.
/// Handles validation and API calls for updating categories.
/// </summary>
public partial class EditCategoryModel : ObservableObject
{
    private readonly CategoryManagementService _service;

    [ObservableProperty]
    private int categoryId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string selectedColor = "#ef4444";

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private string? errorMessage;

    public EditCategoryModel(CategoryManagementService service)
    {
        _service = service;
    }

    /// <summary>
    /// Initialize the model with existing category data.
    /// </summary>
    public void Initialize(int id, string categoryName, string? color, bool active)
    {
        CategoryId = id;
        Name = categoryName;
        SelectedColor = color ?? "#ef4444";
        IsActive = active;
        ErrorMessage = null;
    }

    /// <summary>
    /// Update the category with the current data.
    /// </summary>
    public async Task<bool> UpdateCategoryAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Category name is required.";
            return false;
        }

        try
        {
            var request = new EditCategoryRequest(CategoryId, Name.Trim(), SelectedColor, IsActive);
            await _service.UpdateCategoryAsync(CategoryId, request);
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to update category: {ex.Message}";
            return false;
        }
    }
}
