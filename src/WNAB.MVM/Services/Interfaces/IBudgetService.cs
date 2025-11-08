namespace WNAB.MVM;

/// <summary>
/// Interface for budget-related calculations and operations.
/// </summary>
public interface IBudgetService
{
    Task<decimal> CalculateReadyToAssign(int month, int year);
}
