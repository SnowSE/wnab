using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for individual transaction split items.
/// Represents a single category allocation within a transaction split.
/// </summary>
public partial class AddTransactionSplitModel : ObservableObject
{
    [ObservableProperty]
    private Category? selectedCategory;

    /// <summary>
    /// Categories plus "Income" option for pickers.
    /// </summary>
    public ObservableCollection<Category> AvailableCategoriesWithIncome { get; set; } = new();

    [ObservableProperty]
    private decimal amount;

    public bool IsIncome => SelectedCategory is null;

    [ObservableProperty]
    private string? notes;

    /// <summary>
    /// Convenience property for display - gets the category name.
    /// </summary>
    public string CategoryName => SelectedCategory?.Name ?? string.Empty;

    /// <summary>
    /// Validates that all required fields are populated.
    /// </summary>
    public bool IsValid => (SelectedCategory != null || IsIncome) && Amount != 0;

    /// <summary>
    /// Gets validation error message if the split is invalid.
    /// </summary>
    public string? ValidationError
    {
        get
        {
            if (SelectedCategory == null && !IsIncome)
                return "Category is required";
            if (Amount == 0)
                return "Amount must be non-zero";
            return null;
        }
    }

    /// <summary>
    /// When category is selected, update computed properties.
    /// </summary>
    partial void OnSelectedCategoryChanged(Category? value)
    {
        // If "Income" option selected, set SelectedCategory to null
        if (value != null && value.Id == 0 && value.Name == "Income")
        {
            SelectedCategory = null;
        }
        else
        {
            SelectedCategory = value;
        }
        OnPropertyChanged(nameof(CategoryName));
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(ValidationError));
        OnPropertyChanged(nameof(IsIncome));
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
