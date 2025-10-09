using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using WNAB.Logic;
using WNAB.Logic.Data;
using WNAB.Maui.NewMainPage;
using WNAB.Maui.Services;

namespace WNAB.Maui;

// LLM-Dev:v3 Updated ViewModel to use CategoryManagementService and create goal-like display from category data
public partial class NewMainPageViewModel : ObservableObject
{
    private readonly IPopupService _popupService;
    private readonly IAuthenticationService _authenticationService;
    private readonly UserManagementService _userService;
    private readonly CategoryManagementService _categoryService;
    private readonly CategoryAllocationManagementService _allocationService;
    public CategoryAllocListViewModel categoryList;

    [ObservableProperty]
    private bool isUserLoggedIn;

    [ObservableProperty]
    private string userDisplayName = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int userId;

    public bool IsUserNotLoggedIn => !IsUserLoggedIn;

    public ObservableCollection<CategoryAllocation> CategoryAllocations { get; } = new();

    public NewMainPageViewModel(IPopupService popupService, IAuthenticationService authservice, UserManagementService userService, CategoryManagementService categoryService, CategoryAllocationManagementService allocationService)
    {
        _popupService = popupService;
        _userService = userService;
        _categoryService = categoryService;
        _allocationService = allocationService;
        _authenticationService = authservice;

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
        await RefreshUserId();
        if (IsUserLoggedIn)
        {
            await LoadBudgetData();
        }
    }

    [RelayCommand]
    private async Task RefreshUserId()
    {
        try
        {
            var isAuthenticated = await _authenticationService.IsAuthenticatedAsync();
            if (isAuthenticated)
            {
                var userName = await _authenticationService.GetUserNameAsync();
                IsUserLoggedIn = true;
            }
            else
            {
                IsUserLoggedIn = false;
            }
        }
        catch
        {
            // If authentication check fails, degrade gracefully
            IsUserLoggedIn = false;
        }
    }


    // LLM-Dev:v3 Updated to load categories and calculate goal-like progress from allocations and transaction splits
    private async Task LoadBudgetData()
    {
        if (IsBusy || !IsUserLoggedIn) return;

        try
        {
            IsBusy = true;
            CategoryAllocations.Clear();

            var categories = await _categoryService.GetCategoriesForUserAsync();
            // for each category, get the allocations
            foreach (var cat in categories)
            {
                // either use the returned transfer objects or create new ones and pass.
                var allocations = await _categoryService.GetAllocationsAsync(cat.Id);
                // validate

                //
                foreach (var alloc in allocations)
                {
                    CategoryAllocations.Add(alloc);
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

    // navigate to the transactions page (not implemented yet)
    //[RelayCommand]
    //private async Task NavigateToTransactions()
    //{
    //    await Shell.Current.GoToAsync("Transactions");
    //}

    [RelayCommand]
    private async Task Refresh()
    {
        await InitializeAsync();
    }
}
