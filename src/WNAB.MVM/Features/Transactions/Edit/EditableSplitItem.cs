using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;

namespace WNAB.MVM;

/// <summary>
/// Represents a transaction split that can be edited inline within the transaction edit form.
/// </summary>
public partial class EditableSplitItem : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private int? categoryAllocationId;

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string categoryName = string.Empty;

    [ObservableProperty]
    private bool isNew;

    /// <summary>
    /// When true, this split is marked for deletion and will be deleted on Save.
    /// The UI should hide or strike-through marked splits.
    /// </summary>
    [ObservableProperty]
    private bool isMarkedForDeletion;

    public EditableSplitItem()
    {
        IsNew = true;
    }

    public EditableSplitItem(int id, int? categoryAllocationId, Category? category, decimal amount, string? description, string? initialCategoryName = null)
    {
        IsNew = false;
        Id = id;
        CategoryAllocationId = categoryAllocationId;
        Amount = amount;
        Description = description ?? string.Empty;
        CategoryName = initialCategoryName ?? category?.Name ?? string.Empty;

        // Set SelectedCategory last so that bindings update after other fields are ready
        SelectedCategory = category;
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        CategoryName = value?.Name ?? string.Empty;
    }
}
