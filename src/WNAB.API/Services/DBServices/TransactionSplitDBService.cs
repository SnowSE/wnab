using Microsoft.EntityFrameworkCore;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.API;

public class TransactionSplitDBService
{
    private readonly WnabContext _db;
    private readonly ILogger<TransactionSplitDBService> logger;

    public TransactionSplitDBService(WnabContext db, ILogger<TransactionSplitDBService> logger )
    {
        _db = db;
        this.logger = logger;
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
            .Include(ts => ts.Transaction)
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
                ts.Transaction.TransactionDate,
                ts.CategoryAllocation != null ? ts.CategoryAllocation.Category.Name : "Income",
                ts.Amount,
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
            .Include(ts => ts.Transaction)
            .Where(ts => ts.Id == splitId && ts.Transaction.Account.UserId == userId)
            .Select(ts => new TransactionSplitResponse(
                ts.Id,
                ts.CategoryAllocationId,
                ts.TransactionId,
                ts.Transaction.TransactionDate,
                ts.CategoryAllocation != null ? ts.CategoryAllocation.Category.Name : "Income",
                ts.Amount,
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
        int? categoryAllocationId,
        int userId,
        decimal amount,
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

        CategoryAllocation? allocation = null;
        if (categoryAllocationId is not null)
        {
            // Validate category allocation belongs to user's categories and load it
            allocation = await _db.Allocations
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == categoryAllocationId && a.Category.UserId == userId, cancellationToken);

            if (allocation is null)
                throw new InvalidOperationException("Category allocation does not belong to user");
        }

        var utcNow = DateTime.UtcNow;

        var split = new TransactionSplit
        {
            TransactionId = transactionId,
            CategoryAllocationId = categoryAllocationId,
            Amount = amount,
            Description = notes,
            Transaction = transaction,
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
            transaction.TransactionDate,
            allocation?.Category.Name ?? "Income",
            split.Amount,
            split.Description
        );
    }

    /// <summary>
    /// Updates an existing transaction split
    /// </summary>
    public async Task<TransactionSplitResponse> UpdateTransactionSplitAsync(
        int splitId,
        int userId,
        int? categoryAllocationId,
        decimal amount,
        string? description,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[TransactionSplitDBService] UpdateTransactionSplitAsync called: SplitId={SplitId}, UserId={UserId}, CategoryAllocationId={AllocationId}, Amount={Amount}, Description={Description}", 
            splitId, userId, categoryAllocationId, amount, description);

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

        logger.LogInformation("[TransactionSplitDBService] Found split: CurrentAllocationId={CurrentAllocationId}, NewAllocationId={NewAllocationId}", 
            split.CategoryAllocationId, categoryAllocationId);

        // Validate new allocation belongs to user's categories if allocation is being changed
        // Skip validation if new categoryAllocationId is null (Income)
        if (split.CategoryAllocationId != categoryAllocationId && categoryAllocationId.HasValue)
        {
            logger.LogInformation("[TransactionSplitDBService] Allocation is changing from {OldId} to {NewId}, validating new allocation belongs to user", 
                split.CategoryAllocationId, categoryAllocationId);
            
            var newAllocationBelongsToUser = await _db.Allocations
                .AnyAsync(a => a.Id == categoryAllocationId && a.Category.UserId == userId, cancellationToken);
            
            if (!newAllocationBelongsToUser)
            {
                logger.LogError("[TransactionSplitDBService] New allocation {AllocationId} does not belong to user {UserId}", categoryAllocationId, userId);
                throw new InvalidOperationException("New category allocation does not belong to user");
            }
            
            logger.LogInformation("[TransactionSplitDBService] New allocation validated successfully");
        }

        var utcNow = DateTime.UtcNow;

        // Update split properties
        logger.LogInformation("[TransactionSplitDBService] Updating split properties: CategoryAllocationId={AllocationId}, Amount={Amount}, Description={Description}", 
            categoryAllocationId, amount, description);
        
        split.CategoryAllocationId = categoryAllocationId;
        split.Amount = amount;
        split.Description = description;
        split.UpdatedAt = utcNow;

        logger.LogInformation("[TransactionSplitDBService] Calling SaveChangesAsync...");
        var affected = await _db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[TransactionSplitDBService] SaveChangesAsync completed, affected rows: {Affected}", affected);

        // Postcondition: ensure only the split was updated
        if (affected != 1)
            throw new InvalidOperationException($"Expected to update exactly 1 entry, but updated {affected}.");

        // Reload to get updated category name if CategoryAllocation exists
        string categoryName = "Income"; // Default for null CategoryAllocation
        if (split.CategoryAllocationId.HasValue)
        {
            await _db.Entry(split).Reference(s => s.CategoryAllocation).LoadAsync(cancellationToken);
            if (split.CategoryAllocation != null)
            {
                await _db.Entry(split.CategoryAllocation).Reference(ca => ca.Category).LoadAsync(cancellationToken);
                categoryName = split.CategoryAllocation.Category.Name;
            }
        }

        logger.LogInformation("[TransactionSplitDBService] Returning response: SplitId={SplitId}, CategoryAllocationId={AllocationId}, CategoryName={CategoryName}", 
            split.Id, split.CategoryAllocationId, categoryName);

        return new TransactionSplitResponse(
            split.Id,
            split.CategoryAllocationId,
            split.TransactionId,
            split.Transaction.TransactionDate,
            categoryName,
            split.Amount,
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

    public async Task<List<TransactionSplitResponse>> GetTransactionSplitsForUserByMonthAsync(int id, int month, int year)
    {
        logger.LogTrace("Getting transaction splits for user {UserId} for {Month}/{Year}", id, month, year);
        return await _db.TransactionSplits
            .Include(ts => ts.Transaction)
            .Where(ts => ts.Transaction.Account.UserId == id &&
                         ts.Transaction.TransactionDate.Month == month &&
                         ts.Transaction.TransactionDate.Year == year)
            .OrderByDescending(ts => ts.Transaction.TransactionDate)
            .Select(ts => new TransactionSplitResponse(
                ts.Id,
                ts.CategoryAllocationId,
                ts.TransactionId,
                ts.Transaction.TransactionDate,
                ts.CategoryAllocation != null ? ts.CategoryAllocation.Category.Name : "Income",
                ts.Amount,
                ts.Description
            ))
            .AsNoTracking()
            .ToListAsync();
    }
}
