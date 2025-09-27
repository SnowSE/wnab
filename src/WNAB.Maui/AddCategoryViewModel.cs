using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic; // LLM-Dev: Use shared Logic service for creating categories

namespace WNAB.Maui;

public partial class AddCategoryViewModel : ObservableObject
{
    private readonly CategoryManagementService _categories;

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private int userId;

    public AddCategoryViewModel(CategoryManagementService categories)
    {
        _categories = categories;
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(Name) || UserId <= 0)
            return;

        // Build DTO and send via service
        var record = CategoryManagementService.CreateCategoryRecord(Name, UserId);
        await _categories.CreateCategoryAsync(record);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}