using WNAB.Data;
namespace WNAB.MVM;

public interface IBudgetSnapshotService
{
    Task<BudgetSnapshot?> GetSnapshotAsync(int month, int year);
    Task SaveSnapshotAsync(BudgetSnapshot snapshot);
}