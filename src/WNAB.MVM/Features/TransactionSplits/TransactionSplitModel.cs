using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for individual transaction split items.
/// Represents a single category allocation within a transaction split.
/// </summary>
public partial class TransactionSplitModel : ObservableObject
{
    [ObservableProperty]
    private int transactionId;

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private CategoryAllocation? selectedCategoryAllocation;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private bool isIncome;

    [ObservableProperty]
    private string? notes;

    /// <summary>
    /// Track category allocation ID for API submission.
    /// Enforces budget-first approach - if no allocation exists, validation should prevent saving.
    /// </summary>
    public int CategoryAllocationId => SelectedCategoryAllocation?.Id ?? 0;

    /// <summary>
    /// Convenience property for display - gets the category name from the allocation.
    /// </summary>
    public string CategoryName => SelectedCategoryAllocation?.Category?.Name ?? SelectedCategory?.Name ?? string.Empty;

    /// <summary>
    /// Validates that all required fields are populated for API submission.
    /// </summary>
    public bool IsValid => CategoryAllocationId > 0 && Amount != 0;

    /// <summary>
    /// Gets validation error message if the split is invalid.
    /// </summary>
    public string? ValidationError
    {
        get
        {
            if (CategoryAllocationId <= 0)
                return "Category allocation is required";
            if (Amount == 0)
                return "Amount must be non-zero";
            return null;
        }
    }

    /// <summary>
    /// When category allocation is selected, update computed properties.
    /// </summary>
    partial void OnSelectedCategoryAllocationChanged(CategoryAllocation? value)
    {
        OnPropertyChanged(nameof(CategoryAllocationId));
        OnPropertyChanged(nameof(CategoryName));
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(ValidationError));
    }

    /// <summary>
    /// When category is selected directly, update computed properties.
    /// </summary>
    partial void OnSelectedCategoryChanged(Category? value)
    {
        OnPropertyChanged(nameof(CategoryName));
    }

    /// <summary>
    /// When amount changes, update validation state.
    /// </summary>
    partial void OnAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(ValidationError));
    }
}
