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
    /// Gets a budget snapshot for a specific month and year, filtered by user
    /// </summary>
    public async Task<BudgetSnapshot?> GetSnapshotAsync(int month, int year, int userId, CancellationToken cancellationToken = default)
    {
        return await _db.BudgetSnapshots
            .Include(s => s.Categories)
            .ThenInclude(c => c.Category)
            .FirstOrDefaultAsync(s => s.Month == month && s.Year == year && s.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Saves a new budget snapshot or updates an existing one
    /// </summary>
    public async Task<BudgetSnapshot> SaveSnapshotAsync(BudgetSnapshot snapshot, int userId, CancellationToken cancellationToken = default)
    {
        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting snapshot save.");

        var existingSnapshot = await _db.BudgetSnapshots
            .Include(s => s.Categories)
            .FirstOrDefaultAsync(s => s.Month == snapshot.Month && s.Year == snapshot.Year && s.UserId == userId, cancellationToken);

        if (existingSnapshot is not null)
        {
            // Update existing snapshot
            existingSnapshot.SnapshotReadyToAssign = snapshot.SnapshotReadyToAssign;
            existingSnapshot.IsValid = true; // Mark as valid after rebuild
            
            // Remove old categories and add new ones
            _db.RemoveRange(existingSnapshot.Categories);
            existingSnapshot.Categories = snapshot.Categories;
        }
        else
        {
            // Add new snapshot with userId
            snapshot.UserId = userId;
            snapshot.IsValid = true; // New snapshots are valid
            _db.BudgetSnapshots.Add(snapshot);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return existingSnapshot ?? snapshot;
    }

    public async Task InvalidateSnapshotsFromMonthAsync(int month, int year, int userId, CancellationToken cancellationToken = default)
    {
        // Invalidate the specified month and all future months
        var snapshots = await _db.BudgetSnapshots
            .Where(s => s.UserId == userId &&
                       (s.Year > year || (s.Year == year && s.Month >= month)))
            .ToListAsync(cancellationToken);

        foreach (var snapshot in snapshots)
        {
            snapshot.IsValid = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
