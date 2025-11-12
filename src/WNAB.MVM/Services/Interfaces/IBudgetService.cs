namespace WNAB.MVM;

/// <summary>
/// Interface for budget-related calculations and operations.
/// </summary>
public interface IBudgetService
{
    Task<decimal> CalculateReadyToAssign(int month, int year, BudgetService.BudgetSnapshot? snapshot, DateTime? accountCreationDate);
    Task<BudgetService.BudgetSnapshot> RebuildSnapshots(BudgetService.BudgetSnapshot? previousSnapshot, int targetMonth, int targetYear, DateTime? accountCreationDate);
}
