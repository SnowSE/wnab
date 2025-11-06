using Microsoft.EntityFrameworkCore;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.API;

public class TransactionSplitDBService
{
    private readonly WnabContext _db;

    public TransactionSplitDBService(WnabContext db)
    {
        _db = db;
    }

    public WnabContext DbContext => _db;

    /// <summary>
    /// Gets all transaction splits for a user, optionally filtered by allocation
    /// </summary>
    public async Task<List<TransactionSplitResponse>> GetTransactionSplitsForUserAsync(
        int userId,
        int? allocationId = null,
        CancellationToken cancellationToken = default)
    {
        // Base query: all splits for transactions that belong to the current user's accounts
        var query = _db.TransactionSplits
            .Where(ts => ts.Transaction.Account.UserId == userId);

        if (allocationId.HasValue)
        {
            query = query.Where(ts => ts.CategoryAllocationId == allocationId.Value);
        }

        return await query
            .OrderByDescending(ts => ts.Transaction.TransactionDate)
            .Select(ts => new TransactionSplitResponse(
                ts.Id,
                ts.CategoryAllocationId,
                ts.TransactionId,
                ts.CategoryAllocation.Category.Name,
                ts.Amount,
                ts.IsIncome,
                ts.Description
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a single transaction split by ID
    /// </summary>
    public async Task<TransactionSplitResponse?> GetTransactionSplitByIdAsync(
        int splitId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.TransactionSplits
            .Where(ts => ts.Id == splitId && ts.Transaction.Account.UserId == userId)
            .Select(ts => new TransactionSplitResponse(
                ts.Id,
                ts.CategoryAllocationId,
                ts.TransactionId,
                ts.CategoryAllocation.Category.Name,
                ts.Amount,
                ts.IsIncome,
                ts.Description
            ))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new transaction split (adds split to existing transaction)
    /// </summary>
    public async Task<TransactionSplitResponse> CreateTransactionSplitAsync(
        int transactionId,
        int categoryAllocationId,
        int userId,
        decimal amount,
        bool isIncome,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting transaction split creation.");

        // Validate transaction exists and belongs to user
        var transaction = await _db.Transactions
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Account.UserId == userId, cancellationToken);
        
        if (transaction is null)
            throw new InvalidOperationException("Transaction not found or does not belong to user");

        // Validate category allocation belongs to user's categories and load it
        var allocation = await _db.Allocations
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == categoryAllocationId && a.Category.UserId == userId, cancellationToken);
        
        if (allocation is null)
            throw new InvalidOperationException("Category allocation does not belong to user");

        var utcNow = DateTime.UtcNow;

        var split = new TransactionSplit
        {
            TransactionId = transactionId,
            CategoryAllocationId = categoryAllocationId,
            Amount = amount,
            IsIncome = isIncome,
            Description = notes,
            Transaction = transaction,
            CategoryAllocation = allocation,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        _db.TransactionSplits.Add(split);
        var affected = await _db.SaveChangesAsync(cancellationToken);

        // Postcondition: ensure only the split was saved
        if (affected != 1)
            throw new InvalidOperationException($"Expected to save exactly 1 entry, but saved {affected}.");

        return new TransactionSplitResponse(
            split.Id,
            split.CategoryAllocationId,
            split.TransactionId,
            allocation.Category.Name,
            split.Amount,
            split.IsIncome,
            split.Description
        );
    }

    /// <summary>
    /// Updates an existing transaction split
    /// </summary>
    public async Task<TransactionSplitResponse> UpdateTransactionSplitAsync(
        int splitId,
        int userId,
        int categoryAllocationId,
        decimal amount,
        bool isIncome,
        string? description,
        CancellationToken cancellationToken = default)
    {
        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting transaction split update.");

        // Find split and verify ownership through transaction->account
        var split = await _db.TransactionSplits
            .Include(ts => ts.Transaction)
                .ThenInclude(t => t.Account)
            .Include(ts => ts.CategoryAllocation)
                .ThenInclude(ca => ca.Category)
            .FirstOrDefaultAsync(ts => ts.Id == splitId && ts.Transaction.Account.UserId == userId, cancellationToken);

        if (split is null)
            throw new InvalidOperationException("Transaction split not found or does not belong to user");

        // Validate new allocation belongs to user's categories if allocation is being changed
        if (split.CategoryAllocationId != categoryAllocationId)
        {
            var newAllocationBelongsToUser = await _db.Allocations
                .AnyAsync(a => a.Id == categoryAllocationId && a.Category.UserId == userId, cancellationToken);
            
            if (!newAllocationBelongsToUser)
                throw new InvalidOperationException("New category allocation does not belong to user");
        }

        var utcNow = DateTime.UtcNow;

        // Update split properties
        split.CategoryAllocationId = categoryAllocationId;
        split.Amount = amount;
        split.IsIncome = isIncome;
        split.Description = description;
        split.UpdatedAt = utcNow;

        var affected = await _db.SaveChangesAsync(cancellationToken);

        // Postcondition: ensure only the split was updated
        if (affected != 1)
            throw new InvalidOperationException($"Expected to update exactly 1 entry, but updated {affected}.");

        // Reload to get updated category name
        await _db.Entry(split).Reference(s => s.CategoryAllocation).LoadAsync(cancellationToken);
        await _db.Entry(split.CategoryAllocation).Reference(ca => ca.Category).LoadAsync(cancellationToken);

        return new TransactionSplitResponse(
            split.Id,
            split.CategoryAllocationId,
            split.TransactionId,
            split.CategoryAllocation.Category.Name,
            split.Amount,
            split.IsIncome,
            split.Description
        );
    }

    /// <summary>
    /// Deletes a transaction split
    /// </summary>
    public async Task<bool> DeleteTransactionSplitAsync(
        int splitId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting transaction split deletion.");

        var split = await _db.TransactionSplits
            .Include(ts => ts.Transaction)
                .ThenInclude(t => t.Account)
            .FirstOrDefaultAsync(ts => ts.Id == splitId && ts.Transaction.Account.UserId == userId, cancellationToken);

        if (split is null)
            return false;

        _db.TransactionSplits.Remove(split);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Verifies that a transaction split belongs to a user (through transaction->account)
    /// </summary>
    public async Task<bool> TransactionSplitBelongsToUserAsync(
        int splitId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.TransactionSplits
            .AnyAsync(ts => ts.Id == splitId && ts.Transaction.Account.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Verifies that an allocation belongs to a user (through category ownership)
    /// </summary>
    public async Task<bool> AllocationBelongsToUserAsync(
        int allocationId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Allocations
            .AnyAsync(a => a.Id == allocationId && a.Category.UserId == userId, cancellationToken);
    }
}
