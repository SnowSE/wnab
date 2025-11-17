namespace WNAB.MVM;

using WNAB.Data;

/// <summary>
/// Interface for budget-related calculations and operations.
/// </summary>
public interface IBudgetService
{
    Task<decimal> CalculateReadyToAssign(int month, int year, BudgetSnapshot? snapshot);
    Task<BudgetSnapshot> RebuildSnapshots(BudgetSnapshot? previousSnapshot, int targetMonth, int targetYear);
}
