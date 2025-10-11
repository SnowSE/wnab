using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Services;

namespace WNAB.MVM;

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

        // Basic validation
        if (string.IsNullOrWhiteSpace(Name))
            return;

        // Build DTO and send via service (userId comes from auth token)
        var record = new CategoryRecord(Name);
        await _categories.CreateCategoryAsync(record);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}