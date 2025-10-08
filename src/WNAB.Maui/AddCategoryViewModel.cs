using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using WNAB.Logic; // LLM-Dev: Use shared Logic service for creating categories
using WNAB.Maui.Services;

namespace WNAB.Maui;

public partial class AddCategoryViewModel : ObservableObject
{
    private readonly CategoryManagementService _categories;
    private readonly IAuthenticationService _authService;

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private string name = string.Empty;

    public AddCategoryViewModel(CategoryManagementService categories, IAuthenticationService authService)
    {
        _categories = categories;
        _authService = authService;
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        // Get userId from AuthenticationService for authentication check
        var isAuthenticated = await _authService.IsAuthenticatedAsync();
        if (!isAuthenticated)
        {
            // Handle error - user not logged in
            return;
        }

        // Get userId from SecureStorage
        var userIdString = await SecureStorage.Default.GetAsync("userId");
        if (!int.TryParse(userIdString, out var userId) || userId <= 0)
        {
            // Handle error - unable to get user ID
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