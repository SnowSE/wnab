using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Logic.Data;

namespace WNAB.Maui;

// LLM-Dev:v3 Observable ViewModel for individual transaction split items in UI - updated to use CategoryAllocation
// Represents a single category allocation within a transaction split
public partial class TransactionSplitViewModel : ObservableObject
{
    [ObservableProperty]
    private int transactionId;

    [ObservableProperty]
    private CategoryAllocation? selectedCategoryAllocation;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private bool isIncome;

    [ObservableProperty]
    private string? notes;

    // LLM-Dev:v3 Track category allocation ID for API submission
    public int CategoryAllocationId => SelectedCategoryAllocation?.Id ?? 0;

    // LLM-Dev:v3 Convenience property for display
    public string CategoryName => SelectedCategory?.Name ?? SelectedCategoryAllocation?.Category?.Name ?? string.Empty;

    // LLM-Dev:v3 When category is selected, system should determine CategoryAllocation based on transaction date
    // This enforces budget-first approach - if no allocation exists, validation should prevent saving
    partial void OnSelectedCategoryChanged(Category? value)
    {
        OnPropertyChanged(nameof(CategoryAllocationId));
        OnPropertyChanged(nameof(CategoryName));
    }

    partial void OnSelectedCategoryAllocationChanged(CategoryAllocation? value)
    {
        OnPropertyChanged(nameof(CategoryAllocationId));
    }
}
