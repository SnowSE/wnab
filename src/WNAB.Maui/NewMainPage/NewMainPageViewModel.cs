using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WNAB.Logic;
using WNAB.Logic.Data;
using WNAB.Maui.Services;
using WNAB.Maui.NewMainPageModels;

namespace WNAB.Maui;

// LLM-Dev:v4 Refactor to use AllocationProgressModel and compute progress from transaction splits
public partial class NewMainPageViewModel : ObservableObject
{
    private readonly IPopupService _popupService;
    private readonly IAuthenticationService _authenticationService;
    private readonly CategoryManagementService _categoryService;
    private readonly CategoryAllocationManagementService _allocationService;
    private readonly TransactionManagementService _transactionService;

    [ObservableProperty]
    private bool isUserLoggedIn;

    [ObservableProperty]
    private string userDisplayName = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public bool IsUserNotLoggedIn => !IsUserLoggedIn;

    // UI collection of allocation progress rows
    public ObservableCollection<AllocationProgressModel> Allocations { get; } = new();

    public NewMainPageViewModel(
        IPopupService popupService,
        IAuthenticationService authservice,
        CategoryManagementService categoryService,
        CategoryAllocationManagementService allocationService,
        TransactionManagementService transactionService)
    {
        _popupService = popupService;
        _categoryService = categoryService;
        _allocationService = allocationService;
        _transactionService = transactionService;
        _authenticationService = authservice;

        _ = InitializeAsync();
    }

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
                UserDisplayName = userName ?? string.Empty;
                IsUserLoggedIn = true;
                await LoadBudgetData();

            }
        }
        catch
        {
            IsUserLoggedIn = false;
            UserDisplayName = string.Empty;
        }
    }

    // Load all categories and their allocations and compute progress from transaction splits
    private async Task LoadBudgetData()
    {
        if (IsBusy || !IsUserLoggedIn) return;

        try
        {
            IsBusy = true;
            Allocations.Clear();

            var categories = await _categoryService.GetCategoriesForUserAsync();

            // For each category, fetch allocations and spent from splits
            foreach (var cat in categories)
            {
                var allocations = await _allocationService.GetAllocationsForCategoryAsync(cat.Id);

                // Get spent per category from transaction splits
                var splits = await _transactionService.GetTransactionSplitsForCategoryAsync(cat.Id);
                var spentTotal = splits.Sum(s => s.Amount);

                foreach (var alloc in allocations)
                {
                    var model = new AllocationProgressModel(
                        alloc.Id,
                        cat.Id,
                        cat.Name,
                        alloc.Month,
                        alloc.Year,
                        alloc.BudgetedAmount,
                        spentTotal // for now, no month/year filter yet per instructions
                    );
                    Allocations.Add(model);
                }
            }
        }
        catch
        {
            // swallow for now
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(Allocations));
        }
    }

    [RelayCommand]
    private async Task Login()
    {
        try
        {
            await _authenticationService.LoginAsync();
            RefreshUserId();
        }
        catch
        {

        }
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
