using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic;
using Microsoft.Maui.Storage;
using WNAB.Maui.Services;

namespace WNAB.Maui;

// LLM-Dev: ViewModel for PlanBudget page. Follows MVVM pattern by keeping UI logic out of the view.
// LLM-Dev: Loads user-specific categories from CategoryManagementService following the pattern established in CategoriesViewModel
public sealed partial class PlanBudgetViewModel : ObservableObject
{
    private readonly CategoryManagementService _categoryService;
    private readonly IPopupService _popupService;
    private readonly IAuthenticationService _authenticationService;


    // LLM-Dev: Available categories (left column)
    public ObservableCollection<CategoryItem> Categories { get; } = new();
    
    // LLM-Dev v2: Selected categories for the budget plan (center column) with budget amounts
    public ObservableCollection<BudgetCategoryItem> SelectedCategories { get; } = new();
    
    // LLM-Dev: Track IDs of selected categories to filter them from available list
    private readonly HashSet<int> _selectedCategoryIds = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int userId;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private string statusMessage = "Loading...";

    // LLM-Dev: Controls visibility of the categories list (hidden by default)
    [ObservableProperty]
    private bool isCategoriesVisible = false;

    public PlanBudgetViewModel(CategoryManagementService categoryService, IPopupService popupService, IAuthenticationService authenticationService)
    {
        _categoryService = categoryService;
        _popupService = popupService;
        _authenticationService = authenticationService;
    }

    // LLM-Dev: Initialize the view model by checking user session and loading categories
    public async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadCategoriesAsync();
        }
    }

    // LLM-Dev: Check if user is logged in and get user ID from secure storage (following CategoriesViewModel pattern)
    [RelayCommand]
    private async Task CheckUserSessionAsync()
    {
        try
        {
            var isAuthenticated = await _authenticationService.IsAuthenticatedAsync();
            if (isAuthenticated)
            {
                IsLoggedIn = true;

            }
            else
            {
                IsLoggedIn = false;
                Categories.Clear();
            }
        }
        catch
        {
            IsLoggedIn = false;
            Categories.Clear();
            
        }
        // try
        // {
        //     var userIdString = await SecureStorage.Default.GetAsync("userId");
        //     if (!string.IsNullOrWhiteSpace(userIdString) && int.TryParse(userIdString, out var parsedUserId))
        //     {
        //         UserId = parsedUserId;
        //         IsLoggedIn = true;
        //         StatusMessage = $"Logged in as user {UserId}";
        //     }
        //     else
        //     {
        //         IsLoggedIn = false;
        //         StatusMessage = "Please log in to view budget plan";
        //         Categories.Clear();
        //     }
        // }
        // catch
        // {
        //     IsLoggedIn = false;
        //     StatusMessage = "Error checking login status";
        //     Categories.Clear();
        // }
    }

    // LLM-Dev: Load categories for the current user from the service, filtering out already selected ones
    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        if (IsBusy || !IsLoggedIn) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Loading categories...";
            Categories.Clear();

            var items = await _categoryService.GetCategoriesForUserAsync();
            // LLM-Dev: Only add categories that haven't been selected yet
            foreach (var c in items)
            {
                if (!_selectedCategoryIds.Contains(c.Id))
                {
                    Categories.Add(new CategoryItem(c.Id, c.Name));
                }
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

    // LLM-Dev: Refresh command for manual reload
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadCategoriesAsync();
        }
    }
    
    [RelayCommand]
    private async Task NavigateToHome()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
    
    // LLM-Dev v1: Cancel command with confirmation dialog following MVVM pattern
    // LLM-Dev v1: Uses Shell.Current.DisplayAlert for confirmation, consistent with app patterns
    [RelayCommand]
    private async Task CancelAsync()
    {
        bool confirmed = await Shell.Current.DisplayAlert(
            "Cancel Confirmation",
            "Are you sure you want to cancel? Any unsaved changes will be lost.",
            "Yes",
            "No");
            
        if (confirmed)
        {
            await NavigateToHome();
        }
    }

    // LLM-Dev: Toggle categories list visibility
    [RelayCommand]
    private void ToggleCategoriesVisibility()
    {
        IsCategoriesVisible = !IsCategoriesVisible;
    }

    // LLM-Dev v2: Add command to open Add Category popup and add new category to selected list with budget amount
    [RelayCommand]
    private async Task AddCategory()
    {
        // Get current category IDs before showing popup
        var existingIds = new HashSet<int>(Categories.Select(c => c.Id));
        existingIds.UnionWith(SelectedCategories.Select(c => c.Id));
        
        await _popupService.ShowAddCategoryAsync();
        
        // LLM-Dev v2: After popup closes, find the newly created category and add to selected list as BudgetCategoryItem
        try
        {
            var allCategories = await _categoryService.GetCategoriesForUserAsync();
            var newCategory = allCategories.FirstOrDefault(c => !existingIds.Contains(c.Id));
            
            if (newCategory != null)
            {
                var newItem = new BudgetCategoryItem(newCategory.Id, newCategory.Name, 0);
                SelectedCategories.Add(newItem);
                _selectedCategoryIds.Add(newCategory.Id);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding category: {ex.Message}";
        }
    }

    // LLM-Dev v2: Move category from available list to selected list as BudgetCategoryItem
    [RelayCommand]
    private void SelectCategory(CategoryItem category)
    {
        if (category != null && Categories.Contains(category))
        {
            Categories.Remove(category);
            var budgetItem = new BudgetCategoryItem(category.Id, category.Name, 0);
            SelectedCategories.Add(budgetItem);
            _selectedCategoryIds.Add(category.Id);
        }
    }

    // LLM-Dev v2: Move category from selected list back to available list
    [RelayCommand]
    private void DeselectCategory(BudgetCategoryItem category)
    {
        if (category != null && SelectedCategories.Contains(category))
        {
            SelectedCategories.Remove(category);
            Categories.Add(new CategoryItem(category.Id, category.Name));
            _selectedCategoryIds.Remove(category.Id);
        }
    }
}

// LLM-Dev v1: BudgetCategoryItem class for selected categories with budget amounts
// LLM-Dev v1: Extends CategoryItem with BudgetAmount property and implements INotifyPropertyChanged for two-way binding
public sealed partial class BudgetCategoryItem : ObservableObject
{
    public int Id { get; }
    public string Name { get; }

    [ObservableProperty]
    private decimal budgetAmount;

    public BudgetCategoryItem(int id, string name, decimal budgetAmount = 0)
    {
        Id = id;
        Name = name;
        BudgetAmount = budgetAmount;
    }
}
