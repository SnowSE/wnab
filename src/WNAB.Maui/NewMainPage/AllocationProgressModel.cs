using System;

namespace WNAB.Maui.NewMainPageModels;

// UI model representing a single category allocation with progress
public sealed class AllocationProgressModel
{
    public int AllocationId { get; }
    public int CategoryId { get; }
    public string CategoryName { get; }
    public int Month { get; }
    public int Year { get; }

    public decimal BudgetedAmount { get; }
    public decimal SpentAmount { get; }

    public decimal RemainingAmount => BudgetedAmount - SpentAmount;

    // Progress value between 0.0 and 1.0 for ProgressBar
    public double Progress => BudgetedAmount <= 0
        ? 0
        : Math.Clamp((double)(SpentAmount / BudgetedAmount), 0d, 1d);

    public AllocationProgressModel(
        int allocationId,
        int categoryId,
        string categoryName,
        int month,
        int year,
        decimal budgetedAmount,
        decimal spentAmount)
    {
        AllocationId = allocationId;
        CategoryId = categoryId;
        CategoryName = categoryName;
        Month = month;
        Year = year;
        BudgetedAmount = budgetedAmount;
        SpentAmount = spentAmount;
    }
}
