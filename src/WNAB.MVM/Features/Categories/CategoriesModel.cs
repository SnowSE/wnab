using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for Categories feature.
/// Handles data fetching, authentication state, and category list management.
/// </summary>
public partial class CategoriesModel : ObservableObject
{
    private readonly CategoryManagementService _service;
    private readonly IAuthenticationService _authService;

    public ObservableCollection<CategoryItemViewModel> Items { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private string statusMessage = "Loading...";

    public CategoriesModel(CategoryManagementService service, IAuthenticationService authService)
    {
        _service = service;
        _authService = authService;
    }

    /// <summary>
    /// Initialize the model by checking user session and loading categories if authenticated.
    /// </summary>
    public async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadCategoriesAsync();
        }
    }

    /// <summary>
    /// Check if user is logged in and update authentication state.
    /// </summary>
    public async Task CheckUserSessionAsync()
    {
        try
        {
            IsLoggedIn = await _authService.IsAuthenticatedAsync();
            if (IsLoggedIn)
            {
                var userName = await _authService.GetUserNameAsync();
                StatusMessage = $"Logged in as {userName ?? "user"}";
            }
            else
            {
                IsLoggedIn = false;
                StatusMessage = "Please log in to view categories";
                Items.Clear();
            }
        }
        catch
        {
            IsLoggedIn = false;
            StatusMessage = "Error checking login status";
            Items.Clear();
        }
    }

    /// <summary>
    /// Load categories for the current authenticated user.
    /// </summary>
    public async Task LoadCategoriesAsync()
    {
        if (IsBusy || !IsLoggedIn) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Loading categories...";
            Items.Clear();

            var items = await _service.GetCategoriesForUserAsync();
            foreach (var c in items)
            {
                var categoryItem = new CategoryItemViewModel(c);
                Items.Add(categoryItem);
            }

            StatusMessage = items.Count == 0 ? "No categories found" : $"Loaded {items.Count} categories";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading categories: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Refresh categories by checking session and reloading data.
    /// </summary>
    public async Task RefreshAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadCategoriesAsync();
        }
    }

    /// <summary>
    /// Update a category with new name and color.
    /// Returns a tuple with success status and error message (if any).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> UpdateCategoryAsync(int categoryId, string newName, string? newColor, bool isActive)
    {
        try
        {
            var request = new SharedDTOs.EditCategoryRequest(categoryId, newName, newColor, isActive);
            await _service.UpdateCategoryAsync(categoryId, request);
            StatusMessage = "Category updated successfully";
            return (true, null);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error updating category: {ex.Message}";
            StatusMessage = errorMsg;
            return (false, errorMsg);
        }
    }
}
