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

        // Category is optional - splits can be uncategorized or income
        // if (SelectedCategory == null)
        //     return "Please select a category";

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

            int? allocationId = null;

            // Handle different category scenarios
            if (SelectedCategory != null && SelectedCategory.Id > 0)
            {
                // Regular category - find or create allocation at save time
                var allocation = await _allocations.FindOrCreateAllocationAsync(
                    SelectedCategory.Id,
                    TransactionDate.Month,
                    TransactionDate.Year);

                allocationId = allocation.Id;
            }
            else if (SelectedCategory?.Id == -1)
            {
                // Income - null allocation
                allocationId = null;
            }
            else
            {
                // No category selected (uncategorized) - null allocation
                allocationId = null;
            }

            var record = new TransactionSplitRecord(
                allocationId,
                TransactionId,
                Amount,
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
        Notes = null;
        StatusMessage = string.Empty;
        TransactionDate = DateTime.Today;
    }
}
