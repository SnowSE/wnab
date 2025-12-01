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
        var snapshot = await _db.BudgetSnapshots
            .Include(s => s.Categories)
            .ThenInclude(c => c.Category)
            .FirstOrDefaultAsync(s => s.Month == month && s.Year == year && s.UserId == userId, cancellationToken);

        // If snapshot exists and is valid, return it
        if (snapshot is not null && snapshot.IsValid)
            return snapshot;

        // Otherwise rebuild the snapshot in the DB service and return it
        var rebuilt = await RebuildSnapshotsAsync(month, year, userId, cancellationToken);
        return rebuilt;
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

    // --- Snapshot build logic (adapted from client BudgetService) ---

    private async Task<BudgetSnapshot> RebuildSnapshotsAsync(int targetMonth, int targetYear, int userId, CancellationToken cancellationToken = default)
    {
        BudgetSnapshot snapshot;

        var earliestDate = await GetEarliestActivityDateAsync(userId, cancellationToken);
        
        // Check if requesting a month before any activity - if so, treat as first month
        var requestedDate = new DateTime(targetYear, targetMonth, 1);
        var firstActivityDate = new DateTime(earliestDate.Year, earliestDate.Month, 1);
        
        if (requestedDate <= firstActivityDate)
        {
            // This is the first month or before - create first snapshot for the requested month
            snapshot = await CreateFirstSnapshotAsync(targetMonth, targetYear, userId, cancellationToken);
        }
        else
        {
            var (prevMonth, prevYear) = CalculatePreviousMonth(targetMonth, targetYear);

            var previousSnapshot = await GetSnapshotAsync(prevMonth, prevYear, userId, cancellationToken);
            if (previousSnapshot == null || !previousSnapshot.IsValid)
            {
                previousSnapshot = await RebuildSnapshotsAsync(prevMonth, prevYear, userId, cancellationToken);
            }

            snapshot = await CreateNextSnapshotAsync(previousSnapshot, cancellationToken);
        }

        await SaveSnapshotAsync(snapshot, userId, cancellationToken);

        return snapshot;
    }

    private async Task<DateTime> GetEarliestActivityDateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var earliestTransaction = await _db.Transactions
            .Where(t => t.Account.UserId == userId)
            .OrderBy(t => t.TransactionDate)
            .Select(t => t.TransactionDate)
            .FirstOrDefaultAsync(cancellationToken);

        return earliestTransaction == default ? DateTime.UtcNow : earliestTransaction;
    }

    private async Task<BudgetSnapshot> CreateFirstSnapshotAsync(int currentUserId, CancellationToken cancellationToken = default)
    {
        var accountCreationDate = await GetEarliestActivityDateAsync(currentUserId, cancellationToken);
        return await CreateFirstSnapshotAsync(accountCreationDate.Month, accountCreationDate.Year, currentUserId, cancellationToken);
    }

    private async Task<BudgetSnapshot> CreateFirstSnapshotAsync(int month, int year, int currentUserId, CancellationToken cancellationToken = default)
    {
        var income = await GetIncomeForMonthAsync(month, year, currentUserId, cancellationToken);
        var allocations = await GetAllocationsForMonthAsync(month, year, currentUserId, cancellationToken);
        var categoryData = await BuildCategorySnapshotDataAsync(month, year, currentUserId, cancellationToken);

        return new BudgetSnapshot
        {
            Month = month,
            Year = year,
            SnapshotReadyToAssign = income - allocations,
            Categories = categoryData,
            UserId = currentUserId,
            IsValid = true
        };
    }

    private async Task<BudgetSnapshot> CreateNextSnapshotAsync(BudgetSnapshot previousSnapshot, CancellationToken cancellationToken = default)
    {
        var (currentMonth, currentYear) = CalculateNextMonth(previousSnapshot.Month, previousSnapshot.Year);

        var income = await GetIncomeForMonthAsync(currentMonth, currentYear, previousSnapshot.UserId, cancellationToken);
        var allocations = await GetAllocationsForMonthAsync(currentMonth, currentYear, previousSnapshot.UserId, cancellationToken);
        var overspend = CalculateOverspend(previousSnapshot);
        var categoryData = await BuildCategorySnapshotDataAsync(currentMonth, currentYear, previousSnapshot.UserId, cancellationToken);

        return new BudgetSnapshot
        {
            Month = currentMonth,
            Year = currentYear,
            SnapshotReadyToAssign = previousSnapshot.SnapshotReadyToAssign + income - allocations - overspend,
            Categories = categoryData,
            UserId = previousSnapshot.UserId,
            IsValid = true
        };
    }

    private (int month, int year) CalculateNextMonth(int currentMonth, int currentYear)
    {
        var nextMonth = currentMonth + 1;
        var nextYear = currentYear;

        if (nextMonth > 12)
        {
            nextMonth = 1;
            nextYear++;
        }

        return (nextMonth, nextYear);
    }

    private (int month, int year) CalculatePreviousMonth(int currentMonth, int currentYear)
    {
        var prevMonth = currentMonth - 1;
        var prevYear = currentYear;

        if (prevMonth < 1)
        {
            prevMonth = 12;
            prevYear--;
        }

        return (prevMonth, prevYear);
    }

    private decimal CalculateOverspend(BudgetSnapshot snapshot)
    {
        return snapshot.Categories
            .Where(c => c.Available < 0)
            .Sum(c => Math.Abs(c.Available));
    }

    private async Task<decimal> GetIncomeForMonthAsync(int month, int year, int userId, CancellationToken cancellationToken = default)
    {
        // Income is represented by transaction splits without a category allocation
        return await _db.TransactionSplits
            .Include(ts => ts.Transaction)
            .Where(ts => ts.Transaction.Account.UserId == userId &&
                         ts.Transaction.TransactionDate.Month == month &&
                         ts.Transaction.TransactionDate.Year == year &&
                         ts.CategoryAllocationId == null)
            .SumAsync(ts => ts.Amount, cancellationToken);
    }

    private async Task<decimal> GetAllocationsForMonthAsync(int month, int year, int userId, CancellationToken cancellationToken = default)
    {
        return await _db.Allocations
            .Join(_db.Categories, a => a.CategoryId, c => c.Id, (a, c) => new { a, c })
            .Where(x => x.c.UserId == userId && x.a.Month == month && x.a.Year == year)
            .SumAsync(x => x.a.BudgetedAmount, cancellationToken);
    }

    private async Task<List<CategorySnapshotData>> BuildCategorySnapshotDataAsync(int month, int year, int userId, CancellationToken cancellationToken = default)
    {
        var allAllocations = await _db.Allocations
            .Join(_db.Categories, a => a.CategoryId, c => c.Id, (a, c) => new { Allocation = a, Category = c })
            .Where(x => x.Category.UserId == userId)
            .Select(x => x.Allocation)
            .ToListAsync(cancellationToken);

        var categoryData = new List<CategorySnapshotData>();

        var uniqueCategories = allAllocations
            .Where(a => a.Year < year || (a.Year == year && a.Month <= month))
            .Select(a => a.CategoryId)
            .Distinct();

        foreach (var categoryId in uniqueCategories)
        {
            var categoryAllocations = allAllocations
                .Where(a => a.CategoryId == categoryId && (a.Year < year || (a.Year == year && a.Month <= month)))
                .ToList();

            var assignedValue = categoryAllocations
                .Where(a => a.Month == month && a.Year == year)
                .Sum(a => a.BudgetedAmount);

            decimal activity = 0m;
            foreach (var allocation in categoryAllocations)
            {
                var splitsForAllocation = await _db.TransactionSplits
                    .Where(ts => ts.CategoryAllocationId == allocation.Id)
                    .ToListAsync(cancellationToken);

                activity += splitsForAllocation.Sum(s => s.Amount);
            }

            var totalAllocated = categoryAllocations.Sum(a => a.BudgetedAmount);
            var available = totalAllocated - activity;

            categoryData.Add(new CategorySnapshotData
            {
                CategoryId = categoryId,
                AssignedValue = assignedValue,
                Activity = activity,
                Available = available
            });
        }

        return categoryData;
    }
}
