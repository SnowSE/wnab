using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

public partial class EditTransactionSplitModel : ObservableObject
{
    private readonly TransactionManagementService _transactions;
    private readonly CategoryManagementService _categories;
    private readonly CategoryAllocationManagementService _allocations;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private int splitId;

    [ObservableProperty]
    private int? categoryAllocationId;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private DateTime transactionDate;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private CategoryAllocation? selectedCategoryAllocation;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<Category> AvailableCategories { get; } = new();

    public EditTransactionSplitModel(
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

    public async Task LoadSplitAsync(int id)
    {
        SplitId = id;

        try
        {
            var split = await _transactions.GetTransactionSplitByIdAsync(id);

            if (split == null)
            {
                StatusMessage = "Split not found";
                return;
            }

            CategoryAllocationId = split.CategoryAllocationId;
            Amount = split.Amount;
            Description = split.Description;
            TransactionDate = split.TransactionDate;

            SelectedCategory = AvailableCategories.FirstOrDefault(c => c.Name == split.CategoryName);
            StatusMessage = "Ready to edit split";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading split: {ex.Message}";
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

    partial void OnSelectedCategoryChanged(Category? value)
    {
        if (value != null)
        {
            _ = FindAndSetAllocationAsync(value.Id);
        }
    }

    public async Task FindAndSetAllocationAsync(int categoryId)
    {
        try
        {
            var allocation = await _allocations.FindAllocationAsync(
            categoryId,
                TransactionDate.Month,
                TransactionDate.Year);

            SelectedCategoryAllocation = allocation;
            CategoryAllocationId = allocation?.Id ?? 0;

            if (allocation == null)
            {
                StatusMessage = $"No budget allocation found for this category";
            }
            else
            {
                StatusMessage = "Ready to edit split";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error finding allocation: {ex.Message}";
        }
    }

    public string? ValidateForSave()
    {
        if (!IsLoggedIn)
            return "Please log in first";

        if (CategoryAllocationId <= 0)
            return "Please select a category";

        if (Amount == 0)
            return "Please enter an amount";

        return null;
    }

    public async Task<(bool success, string message)> UpdateSplitAsync()
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
            StatusMessage = "Updating split...";

            // Convert -1 (Income) to null for CategoryAllocationId
            int? allocationId = CategoryAllocationId == -1 ? null : (CategoryAllocationId == 0 ? null : CategoryAllocationId);

            var request = new EditTransactionSplitRequest(
                SplitId,
                allocationId,
                Amount,
                Description
            );

            await _transactions.UpdateTransactionSplitAsync(request);
            StatusMessage = "Split updated successfully!";

            return (true, "Split updated successfully!");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error updating split: {ex.Message}";
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
        SplitId = 0;
        CategoryAllocationId = 0;
        Amount = 0;
        Description = null;
        SelectedCategory = null;
        SelectedCategoryAllocation = null;
        StatusMessage = string.Empty;
    }
}
