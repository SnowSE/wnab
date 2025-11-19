using WNAB.Data;

namespace WNAB.API;

public interface IBudgetSnapshotDbService
{
    WnabContext DbContext { get; }
    Task<BudgetSnapshot?> GetSnapshotAsync(int month, int year, int userId, CancellationToken cancellationToken = default);
    Task<BudgetSnapshot> SaveSnapshotAsync(BudgetSnapshot snapshot, int userId, CancellationToken cancellationToken = default);
    Task InvalidateSnapshotsFromMonthAsync(int month, int year, int userId, CancellationToken cancellationToken = default);
}
