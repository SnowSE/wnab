using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic; // LLM-Dev: Use shared CategoryManagementService
using Microsoft.Maui.Storage;

namespace WNAB.Maui;

// LLM-Dev: ViewModel for Categories page. Keeps UI logic out of the view (MVVM).
// LLM-Dev:v4 Updated to follow AccountsViewModel pattern: get userId from SecureStorage and load user-specific categories
// LLM-Dev:v5 Added AddCategory command to open popup from this page
public sealed partial class CategoriesViewModel : ObservableObject
{
    private readonly CategoryManagementService _service;
    private readonly IPopupService _popupService;

    public ObservableCollection<CategoryItem> Categories { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int userId;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private string statusMessage = "Loading...";

    public CategoriesViewModel(CategoryManagementService service, IPopupService popupService)
    {
        _service = service;
        _popupService = popupService;
    }

    // LLM-Dev:v4 Added initialization method following AccountsViewModel pattern
    public async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadCategoriesAsync();
        }
    }

    // LLM-Dev:v4 Check if user is logged in and get user ID from secure storage (following AccountsViewModel pattern)
    [RelayCommand]
    private async Task CheckUserSessionAsync()
    {
        try
        {
            var userIdString = await SecureStorage.Default.GetAsync("userId");
            if (!string.IsNullOrWhiteSpace(userIdString) && int.TryParse(userIdString, out var parsedUserId))
            {
                UserId = parsedUserId;
                IsLoggedIn = true;
                StatusMessage = $"Logged in as user {UserId}";
            }
            else
            {
                IsLoggedIn = false;
                StatusMessage = "Please log in to view categories";
                Categories.Clear();
            }
        }
        catch
        {
            IsLoggedIn = false;
            StatusMessage = "Error checking login status";
            Categories.Clear();
        }
    }

    // LLM-Dev:v4 Updated to use stored user ID and user-specific endpoint
    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        if (IsBusy || !IsLoggedIn || UserId <= 0) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Loading categories...";
            Categories.Clear();
            
            var items = await _service.GetCategoriesForUserAsync(UserId);
            foreach (var c in items)
                Categories.Add(new CategoryItem(c.Id, c.Name));
                
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

    // LLM-Dev:v4 Add refresh command for manual reload (following AccountsViewModel pattern)
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadCategoriesAsync();
        }
    }

    // LLM-Dev:v4 Keep original LoadAsync for backward compatibility, but make it call the new method
    [RelayCommand]
    public async Task LoadAsync()
    {
        await LoadCategoriesAsync();
    }

    // LLM-Dev:v5 Add command to open Add Category popup
    [RelayCommand]
    private async Task AddCategory()
    {
        await _popupService.ShowAddCategoryAsync();
        // Refresh the list after popup closes
        await RefreshAsync();
    }

    // LLM-Dev:v6 Navigation command to return to home page
    [RelayCommand]
    private async Task NavigateToHome()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}

public sealed record CategoryItem(int Id, string Name);