using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Services;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for Add Category feature.
/// Handles validation, authentication checks, and category creation.
/// </summary>
public partial class AddCategoryModel : ObservableObject
{
    private readonly CategoryManagementService _categories;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isSuccessful;

    public AddCategoryModel(CategoryManagementService categories, IAuthenticationService authService)
    {
        _categories = categories;
        _authService = authService;
    }

    /// <summary>
    /// Validates if the category name is valid for creation.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Category name is required";
            return false;
        }

        ErrorMessage = null;
        return true;
    }

    /// <summary>
    /// Validates that the user is authenticated.
    /// </summary>
    public async Task<bool> ValidateAuthenticationAsync()
    {
        try
        {
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                ErrorMessage = "You must be logged in to create a category";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Authentication error: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Creates a new category. Returns true if successful, false otherwise.
    /// </summary>
    public async Task<bool> CreateCategoryAsync()
    {
        if (IsBusy) return false;

        try
        {
            IsBusy = true;
            IsSuccessful = false;
            ErrorMessage = null;

            // Validate authentication
            if (!await ValidateAuthenticationAsync())
                return false;

            // Validate input
            if (!IsValid())
                return false;

            // Build DTO and send via service (userId comes from auth token)
            var record = new CategoryRecord(Name);
            await _categories.CreateCategoryAsync(record);

            IsSuccessful = true;
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating category: {ex.Message}";
            IsSuccessful = false;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Resets the form state for reuse.
    /// </summary>
    public void Reset()
    {
        Name = string.Empty;
        ErrorMessage = null;
        IsSuccessful = false;
        IsBusy = false;
    }
}
