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

    // Note: CategoryAllocationId should be looked up based on the selected category and transaction date,
    // not set directly from the category ID. This is handled by the parent EditTransactionModel.
    // Do not automatically set CategoryAllocationId when SelectedCategory changes.
}
