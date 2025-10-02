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
    private string userIdInput;

    private int ConvertUserId()
    {
        int userId; 
        if (!int.TryParse(userIdInput, out userId)) 
            throw new Exception("User ID input was not a number");
        return userId;
    }

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
        var parseduserID = ConvertUserId();
        // Basic validation
        if (string.IsNullOrWhiteSpace(Name) || parseduserID <= 0)
            return;

        // Build DTO and send via service
        var record = CategoryManagementService.CreateCategoryRecord(Name, parseduserID);
        await _categories.CreateCategoryAsync(record);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}