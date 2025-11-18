namespace WNAB.MVM;

using WNAB.Data;

/// <summary>
/// Interface for budget-related calculations and operations.
/// </summary>
public interface IBudgetService
{
    Task<decimal> CalculateReadyToAssign(int month, int year);
    Task<BudgetSnapshot> RebuildSnapshots(int targetMonth, int targetYear);
}
