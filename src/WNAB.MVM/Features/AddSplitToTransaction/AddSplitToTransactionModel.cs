using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

/// <summary>
/// Business logic for adding a new split to an existing transaction.
/// </summary>
public partial class AddSplitToTransactionModel : ObservableObject
{
    private readonly TransactionManagementService _transactions;
    private readonly CategoryManagementService _categories;
    private readonly CategoryAllocationManagementService _allocations;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private int transactionId;

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private bool isIncome;

    [ObservableProperty]
    private string? notes;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private DateTime transactionDate = DateTime.Today;

    public ObservableCollection<Category> AvailableCategories { get; } = new();

    public AddSplitToTransactionModel(
        TransactionManagementService transactions,
        CategoryManagementService categories,
        CategoryAllocationManagementService allocations,
        IAuthenticationService authService)
    {
        _transactions = transactions;
        _categories = categories;
        _allocations = allocations;
        _authService = authService;
    }

    public async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadCategoriesAsync();
        }
    }

    public async Task LoadTransactionAsync(int id)
    {
        TransactionId = id;

        try
        {
            var transaction = await _transactions.GetTransactionByIdAsync(id);

            if (transaction == null)
            {
                StatusMessage = "Transaction not found";
                return;
            }

            TransactionDate = transaction.TransactionDate;
            StatusMessage = "Ready to add split";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading transaction: {ex.Message}";
        }
    }

    private async Task CheckUserSessionAsync()
    {
        try
        {
            IsLoggedIn = await _authService.IsAuthenticatedAsync();
            if (!IsLoggedIn)
            {
                StatusMessage = "Please log in first";
            }
        }
        catch (Exception ex)
        {
            IsLoggedIn = false;
            StatusMessage = $"Error checking login: {ex.Message}";
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _categories.GetCategoriesForUserAsync();
            AvailableCategories.Clear();
            foreach (var category in categories)
                AvailableCategories.Add(category);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading categories: {ex.Message}";
        }
    }

    public string? ValidateForSave()
    {
        if (!IsLoggedIn)
            return "Please log in first";

        if (TransactionId <= 0)
            return "Invalid transaction";

        if (SelectedCategory == null)
            return "Please select a category";

        if (Amount == 0)
            return "Please enter an amount";

        return null;
    }

    public async Task<(bool success, string message)> CreateSplitAsync()
    {
        var validationError = ValidateForSave();
        if (validationError != null)
        {
            StatusMessage = validationError;
            return (false, validationError);
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Adding split...";

            // Find allocation at save time, not during category selection
            var allocation = await _allocations.FindAllocationAsync(
                SelectedCategory!.Id,
                TransactionDate.Month,
                TransactionDate.Year);

            if (allocation == null)
            {
                var errorMsg = $"No budget allocation found for {SelectedCategory.Name} in {TransactionDate:MMMM yyyy}. Please create a budget first.";
                StatusMessage = errorMsg;
                return (false, errorMsg);
            }

            var record = new TransactionSplitRecord(
                allocation.Id,
                TransactionId,
                Amount,
                IsIncome,
                Notes);

            await _transactions.CreateTransactionSplitAsync(record);
            StatusMessage = "Split added successfully!";

            return (true, "Split added successfully!");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error adding split: {ex.Message}";
            StatusMessage = errorMsg;
            return (false, errorMsg);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Clear()
    {
        TransactionId = 0;
        SelectedCategory = null;
        Amount = 0;
        IsIncome = false;
        Notes = null;
        StatusMessage = string.Empty;
        TransactionDate = DateTime.Today;
    }
}
