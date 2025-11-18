using WNAB.Data;

namespace WNAB.API;

public interface IBudgetSnapshotDbService
{
    Task<BudgetSnapshot?> GetSnapshotAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<BudgetSnapshot> SaveSnapshotAsync(BudgetSnapshot snapshot, CancellationToken cancellationToken = default);
}
