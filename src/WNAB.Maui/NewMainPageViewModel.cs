using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using WNAB.Logic;
using WNAB.Logic.Data;

namespace WNAB.Maui;

// LLM-Dev:v3 Updated ViewModel to use CategoryManagementService and create goal-like display from category data
public partial class NewMainPageViewModel : ObservableObject
{
    private readonly IPopupService _popupService;
    private readonly UserManagementService _userService;
    private readonly CategoryManagementService _categoryService;
    private readonly CategoryAllocationManagementService _allocationService;

    [ObservableProperty]
    private string title = "New Main Page";

    [ObservableProperty]
    private bool isUserLoggedIn;

    [ObservableProperty]
    private string userDisplayName = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int userId;

    public bool IsUserNotLoggedIn => !IsUserLoggedIn;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<CategoryAllocation> CategoryAllocations { get; } = new();

    public NewMainPageViewModel(IPopupService popupService, UserManagementService userService, CategoryManagementService categoryService, CategoryAllocationManagementService allocationService)
    {
        _popupService = popupService;
        _userService = userService;
        _categoryService = categoryService;
        _allocationService = allocationService;
        
        // Initialize user session on construction
        _ = InitializeAsync();
    }

    // LLM-Dev:v1 Added property change notification for IsUserNotLoggedIn when IsUserLoggedIn changes
    partial void OnIsUserLoggedInChanged(bool value)
    {
        OnPropertyChanged(nameof(IsUserNotLoggedIn));
    }

    private async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
        if (IsUserLoggedIn)
        {
            await LoadUserDataAsync();
            await LoadCategoriesWithGoalsAsync();
        }
    }

    private async Task CheckUserSessionAsync()
    {
        try
        {
            var userIdString = await SecureStorage.Default.GetAsync("userId");
            if (!string.IsNullOrWhiteSpace(userIdString) && int.TryParse(userIdString, out var parsedUserId))
            {
                UserId = parsedUserId;
                IsUserLoggedIn = true;
            }
            else
            {
                IsUserLoggedIn = false;
                UserDisplayName = string.Empty;
                UserId = 0;
            }
        }
        catch
        {
            IsUserLoggedIn = false;
            UserDisplayName = string.Empty;
            UserId = 0;
        }
    }

    private async Task LoadUserDataAsync()
    {
        try
        {
            if (UserId > 0)
            {
                var user = await _userService.GetUserByIdAsync(UserId);
                if (user != null)
                {
                    UserDisplayName = $"{user.FirstName} {user.LastName}";
                }
            }
        }
        catch
        {
            UserDisplayName = "Unknown User";
        }
    }

    // LLM-Dev:v3 Updated to load categories and calculate goal-like progress from allocations and transaction splits
    private async Task LoadCategoriesWithGoalsAsync()
    {
        if (IsBusy || !IsUserLoggedIn || UserId <= 0) return;

        try
        {
            IsBusy = true;
            Categories.Clear();

            var categories = await _categoryService.GetCategoriesForUserAsync(UserId);
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            foreach (var category in categories)
            {
                Categories.Add(category);
                // Get allocations for this category for current month
                var allocations = await _allocationService.GetAllocationsForCategoryAsync(category.Id);
                
                foreach (var allocation in allocations)
                {
                    CategoryAllocations.Add(allocation);
                }

            }
        }
        catch
        {
            // Handle error silently for now
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(Categories));
            OnPropertyChanged(nameof(CategoryAllocations)); 
        }
    }

    [RelayCommand]
    private async Task Login()
    {
        await _popupService.ShowLoginAsync();
        
        // Refresh after login popup closes
        await InitializeAsync();
    }

    [RelayCommand]
    private async Task NavigateToTransactions()
    {
        await Shell.Current.GoToAsync("Transactions");
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await InitializeAsync();
    }
}

// LLM-Dev:v3 Created CategoryGoalItem model to represent categories as goal-like items with progress
public sealed record CategoryGoalItem(string Name, decimal Goal, decimal SoFar)
{
    public double ProgressPercentage => Goal > 0 ? Math.Min((double)(SoFar / Goal), 1.0) : 0.0;
}
