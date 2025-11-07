using Microsoft.EntityFrameworkCore;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.API;

public class AllocationDBService
{
    private readonly WnabContext _db;

    public AllocationDBService(WnabContext db)
    {
        _db = db;
    }

    public WnabContext DbContext => _db;

    /// <summary>
    /// Gets all allocations for a specific category
    /// </summary>
    public async Task<List<CategoryAllocationResponse>> GetAllocationsForCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _db.Allocations
            .Where(a => a.CategoryId == categoryId)
            .Select(a => new CategoryAllocationResponse(
                a.Id,
                a.CategoryId,
                a.BudgetedAmount,
                a.Month,
                a.Year,
                a.EditorName,
                a.PercentageAllocation,
                a.OldAmount,
                a.EditedMemo,
                a.IsActive,
                a.CreatedAt,
                a.UpdatedAt
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CategoryAllocationResponse>> GetAllocationsForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _db.Allocations
            .Join(_db.Categories, a => a.CategoryId, c => c.Id, (a, c) => new { Allocation = a, Category = c })
            .Where(x => x.Category.UserId == userId)
            .Select(x => new CategoryAllocationResponse(
                x.Allocation.Id,
                x.Allocation.CategoryId,
                x.Allocation.BudgetedAmount,
                x.Allocation.Month,
                x.Allocation.Year,
                x.Allocation.EditorName,
                x.Allocation.PercentageAllocation,
                x.Allocation.OldAmount,
                x.Allocation.EditedMemo,
                x.Allocation.IsActive,
                x.Allocation.CreatedAt,
                x.Allocation.UpdatedAt
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new allocation for a category
    /// </summary>
    public async Task<CategoryAllocation> CreateAllocationAsync(
        int categoryId,
        decimal budgetedAmount,
        int month,
        int year,
        string? editorName = null,
        decimal? percentageAllocation = null,
        decimal? oldAmount = null,
        string? editedMemo = null,
        CancellationToken cancellationToken = default)
    {
        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting allocation creation.");

        // Check if an allocation already exists for this category, month, and year
        var existingAllocation = await _db.Allocations
            .FirstOrDefaultAsync(a => a.CategoryId == categoryId && a.Month == month && a.Year == year && a.IsActive, cancellationToken);
        
        if (existingAllocation is not null)
            throw new InvalidOperationException($"An allocation for this category already exists for {month}/{year}");

        var allocation = new CategoryAllocation
        {
            CategoryId = categoryId,
            BudgetedAmount = budgetedAmount,
            Month = month,
            Year = year,
            EditorName = editorName,
            PercentageAllocation = percentageAllocation,
            OldAmount = oldAmount,
            EditedMemo = editedMemo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Allocations.Add(allocation);
        var affected = await _db.SaveChangesAsync(cancellationToken);

        // Postcondition: ensure only the allocation was saved
        if (affected != 1)
            throw new InvalidOperationException($"Expected to save exactly 1 entry, but saved {affected}.");

        return allocation;
    }

    /// <summary>
    /// Updates an existing allocation
    /// </summary>
    public async Task<CategoryAllocation> UpdateAllocationAsync(
        int allocationId,
        decimal? budgetedAmount = null,
        bool? isActive = null,
        string? editorName = null,
        string? editedMemo = null,
        CancellationToken cancellationToken = default)
    {
        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting allocation update.");

        var allocation = await _db.Allocations
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == allocationId, cancellationToken);

        if (allocation is null)
            throw new InvalidOperationException("Allocation not found");

        // Update only provided fields
        if (budgetedAmount.HasValue)
        {
            allocation.OldAmount = allocation.BudgetedAmount;
            allocation.BudgetedAmount = budgetedAmount.Value;
        }

        if (isActive.HasValue)
            allocation.IsActive = isActive.Value;

        if (editorName is not null)
            allocation.EditorName = editorName;

        if (editedMemo is not null)
            allocation.EditedMemo = editedMemo;

        allocation.UpdatedAt = DateTime.UtcNow;

        var affected = await _db.SaveChangesAsync(cancellationToken);

        // Postcondition: ensure only the allocation was updated
        if (affected != 1)
            throw new InvalidOperationException($"Expected to update exactly 1 entry, but updated {affected}.");

        return allocation;
    }

    /// <summary>
    /// Verifies that an allocation belongs to a user (through category ownership)
    /// </summary>
    public async Task<bool> AllocationBelongsToUserAsync(int allocationId, int userId, CancellationToken cancellationToken = default)
    {
        return await _db.Allocations
            .AnyAsync(a => a.Id == allocationId && a.Category.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Gets an allocation by ID with its category information
    /// </summary>
    public async Task<CategoryAllocation?> GetAllocationByIdAsync(int allocationId, CancellationToken cancellationToken = default)
    {
        return await _db.Allocations
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == allocationId, cancellationToken);
    }
}
