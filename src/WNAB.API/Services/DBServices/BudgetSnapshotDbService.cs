using Microsoft.EntityFrameworkCore;
using WNAB.Data;

namespace WNAB.API;

public class BudgetSnapshotDbService : IBudgetSnapshotDbService
{
    private readonly WnabContext _db;

    public BudgetSnapshotDbService(WnabContext db)
    {
        _db = db;
    }

    public WnabContext DbContext => _db;

    /// <summary>
    /// Gets a budget snapshot for a specific month and year
    /// </summary>
    public async Task<BudgetSnapshot?> GetSnapshotAsync(int month, int year, CancellationToken cancellationToken = default)
    {
        return await _db.BudgetSnapshots
            .Include(s => s.Categories)
            .ThenInclude(c => c.Category)
            .FirstOrDefaultAsync(s => s.Month == month && s.Year == year, cancellationToken);
    }

    /// <summary>
    /// Saves a new budget snapshot or updates an existing one
    /// </summary>
    public async Task<BudgetSnapshot> SaveSnapshotAsync(BudgetSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting snapshot save.");

        var existingSnapshot = await _db.BudgetSnapshots
            .Include(s => s.Categories)
            .FirstOrDefaultAsync(s => s.Month == snapshot.Month && s.Year == snapshot.Year, cancellationToken);

        if (existingSnapshot is not null)
        {
            // Update existing snapshot
            existingSnapshot.SnapshotReadyToAssign = snapshot.SnapshotReadyToAssign;
            
            // Remove old categories and add new ones
            _db.RemoveRange(existingSnapshot.Categories);
            existingSnapshot.Categories = snapshot.Categories;
        }
        else
        {
            // Add new snapshot
            _db.BudgetSnapshots.Add(snapshot);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return existingSnapshot ?? snapshot;
    }
}
