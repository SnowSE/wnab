using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Logic.Data;

namespace WNAB.Maui;

// LLM-Dev:v1 Observable model for individual transaction split items in the UI
// Represents a single category allocation within a transaction split
public partial class TransactionSplitItem : ObservableObject
{
    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private string? notes;

    // LLM-Dev:v1 Track category ID for easier API submission
    public int CategoryId => SelectedCategory?.Id ?? 0;

    // LLM-Dev:v1 Update when category selection changes
    partial void OnSelectedCategoryChanged(Category? value)
    {
        OnPropertyChanged(nameof(CategoryId));
    }
}
