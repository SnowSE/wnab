using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic; // LLM-Dev: Use shared Logic service for creating categories
using Microsoft.Maui.Storage;

namespace WNAB.Maui;

public partial class AddCategoryViewModel : ObservableObject
{
    private readonly CategoryManagementService _categories;

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private string name = string.Empty;

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
        // LLM-Dev:v4 Get userId from SecureStorage instead of manual input
        var userIdString = await SecureStorage.Default.GetAsync("userId");
        if (!int.TryParse(userIdString, out var userId) || userId <= 0)
        {
            // Handle error - user not logged in
            return;
        }

        // Basic validation
        if (string.IsNullOrWhiteSpace(Name))
            return;

        // Build DTO and send via service
        var record = CategoryManagementService.CreateCategoryRecord(Name, userId);
        await _categories.CreateCategoryAsync(record);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}