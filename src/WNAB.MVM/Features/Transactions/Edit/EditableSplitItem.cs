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
    private bool isNew;

    public EditableSplitItem()
    {
        IsNew = true;
    }

    public EditableSplitItem(int id, int? categoryAllocationId, Category? category, decimal amount, string? description)
    {
        Id = id;
        CategoryAllocationId = categoryAllocationId;
        SelectedCategory = category;
        Amount = amount;
        Description = description ?? string.Empty;
        IsNew = false;
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        CategoryAllocationId = value?.Id;
    }
}
