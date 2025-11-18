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
        // Get the snapshot and filter categories to only those belonging to this user
        var snapshot = await _db.BudgetSnapshots
            .Include(s => s.Categories)
            .ThenInclude(c => c.Category)
            .FirstOrDefaultAsync(s => s.Month == month && s.Year == year, cancellationToken);

        if (snapshot == null)
            return null;

        // Filter to only categories owned by this user
        snapshot.Categories = snapshot.Categories
            .Where(c => c.Category != null && c.Category.UserId == userId)
            .ToList();

        return snapshot;
    }

    /// <summary>
    /// Saves a new budget snapshot or updates an existing one
    /// Only saves categories that belong to the specified user
    /// </summary>
    public async Task<BudgetSnapshot> SaveSnapshotAsync(BudgetSnapshot snapshot, int userId, CancellationToken cancellationToken = default)
    {
        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting snapshot save.");

        var existingSnapshot = await _db.BudgetSnapshots
            .Include(s => s.Categories)
            .ThenInclude(c => c.Category)
            .FirstOrDefaultAsync(s => s.Month == snapshot.Month && s.Year == snapshot.Year, cancellationToken);

        if (existingSnapshot is not null)
        {
            // Update existing snapshot
            existingSnapshot.SnapshotReadyToAssign = snapshot.SnapshotReadyToAssign;
            
            // Remove old categories that belong to this user
            var userCategories = existingSnapshot.Categories
                .Where(c => c.Category != null && c.Category.UserId == userId)
                .ToList();
            _db.RemoveRange(userCategories);
            
            // Add new categories (they should already be validated to belong to this user)
            existingSnapshot.Categories.AddRange(snapshot.Categories);
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
