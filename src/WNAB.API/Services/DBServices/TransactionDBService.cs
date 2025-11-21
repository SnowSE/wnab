using Microsoft.EntityFrameworkCore;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.API;

public class TransactionDBService
{
    private readonly WnabContext _db;

    public TransactionDBService(WnabContext db)
    {
        _db = db;
    }

    public WnabContext DbContext => _db;

    /// <summary>
    /// Gets all transactions for a user, optionally filtered by account
    /// </summary>
    public async Task<List<TransactionResponse>> GetTransactionsForUserAsync(
        int userId,
        int? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Transactions
            .Where(t => t.Account.UserId == userId);

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new TransactionResponse(
                t.Id,
                t.AccountId,
                t.Account.AccountName,
                t.Payee,
                t.Description,
                t.Amount,
                t.TransactionDate,
                t.IsReconciled,
                t.CreatedAt,
                t.UpdatedAt
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a single transaction by ID
    /// </summary>
    public async Task<TransactionResponse?> GetTransactionByIdAsync(
        int transactionId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Transactions
            .Where(t => t.Id == transactionId && t.Account.UserId == userId)
            .Select(t => new TransactionResponse(
                t.Id,
                t.AccountId,
                t.Account.AccountName,
                t.Payee,
                t.Description,
                t.Amount,
                t.TransactionDate,
                t.IsReconciled,
                t.CreatedAt,
                t.UpdatedAt
            ))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new transaction with splits
    /// </summary>
    public async Task<TransactionResponse> CreateTransactionAsync(
        int accountId,
        int userId,
        string payee,
        string? description,
        decimal amount,
        DateTime transactionDate,
        List<TransactionSplitRecord> splits,
        CancellationToken cancellationToken = default)
    {
        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting transaction creation.");

        // Validate account exists and belongs to user
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);
        
        if (account is null)
            throw new InvalidOperationException("Account not found or does not belong to user");

        // Validate all category allocations belong to user's categories and load them
        // Allow transactions without category allocations (uncategorized)
        var allocationIds = splits
            .Where(s => s.CategoryAllocationId.HasValue)
            .Select(s => s.CategoryAllocationId!.Value)
            .Distinct()
            .ToList();

        var allocations = new List<CategoryAllocation>();
        if (allocationIds.Count > 0)
        {
            allocations = await _db.Allocations
                .Include(a => a.Category)
                .Where(a => allocationIds.Contains(a.Id) && a.Category.UserId == userId)
                .ToListAsync(cancellationToken);

            if (allocations.Count != allocationIds.Count)
                throw new InvalidOperationException("One or more category allocations do not belong to user");
        }

        var utcNow = DateTime.UtcNow;
        var utcTransactionDate = transactionDate.Kind == DateTimeKind.Utc
            ? transactionDate
            : DateTime.SpecifyKind(transactionDate, DateTimeKind.Utc);

        var transaction = new Transaction
        {
            AccountId = accountId,
            Payee = payee,
            Description = description,
            Amount = amount,
            TransactionDate = utcTransactionDate,
            Account = account,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);

        // Create splits only if provided and not all null
        foreach (var splitRecord in splits)
        {
            CategoryAllocation? allocation = null;
            if (splitRecord.CategoryAllocationId.HasValue)
            {
                allocation = allocations.First(a => a.Id == splitRecord.CategoryAllocationId.Value);
            }

            var split = new TransactionSplit
            {
                TransactionId = transaction.Id,
                CategoryAllocationId = splitRecord.CategoryAllocationId,
                Amount = splitRecord.Amount,
                Description = splitRecord.Notes,
                Transaction = transaction,
                CategoryAllocation = allocation,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };
            _db.TransactionSplits.Add(split);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new TransactionResponse(
            transaction.Id,
            transaction.AccountId,
            account.AccountName,
            transaction.Payee,
            transaction.Description,
            transaction.Amount,
            transaction.TransactionDate,
            transaction.IsReconciled,
            transaction.CreatedAt,
            transaction.UpdatedAt
        );
    }

    /// <summary>
    /// Updates an existing transaction
    /// </summary>
    public async Task<TransactionResponse> UpdateTransactionAsync(
        int transactionId,
        int userId,
        int accountId,
        string payee,
        string? description,
        decimal amount,
        DateTime transactionDate,
        bool isReconciled,
        CancellationToken cancellationToken = default)
    {
        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting transaction update.");

        // Find transaction and verify ownership
        var transaction = await _db.Transactions
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Account.UserId == userId, cancellationToken);

        if (transaction is null)
            throw new InvalidOperationException("Transaction not found or does not belong to user");

        // Validate new account belongs to user if account is being changed
        if (transaction.AccountId != accountId)
        {
            var newAccountBelongsToUser = await _db.Accounts
                .AnyAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);
            
            if (!newAccountBelongsToUser)
                throw new InvalidOperationException("New account does not belong to user");
        }

        var utcNow = DateTime.UtcNow;
        var utcTransactionDate = transactionDate.Kind == DateTimeKind.Utc
            ? transactionDate
            : DateTime.SpecifyKind(transactionDate, DateTimeKind.Utc);

        transaction.AccountId = accountId;
        transaction.Payee = payee;
        transaction.Description = description;
        transaction.Amount = amount;
        transaction.TransactionDate = utcTransactionDate;
        transaction.IsReconciled = isReconciled;
        transaction.UpdatedAt = utcNow;

        var affected = await _db.SaveChangesAsync(cancellationToken);

        // Postcondition: ensure only the transaction was updated
        if (affected != 1)
            throw new InvalidOperationException($"Expected to update exactly 1 entry, but updated {affected}.");

        // Reload the Account relationship if it changed to get the correct account name
        if (transaction.Account?.Id != accountId)
        {
            await _db.Entry(transaction).Reference(t => t.Account).LoadAsync(cancellationToken);
        }

        return new TransactionResponse(
            transaction.Id,
            transaction.AccountId,
            transaction.Account.AccountName,
            transaction.Payee,
            transaction.Description,
            transaction.Amount,
            transaction.TransactionDate,
            transaction.IsReconciled,
            transaction.CreatedAt,
            transaction.UpdatedAt
        );
    }

    /// <summary>
    /// Deletes a transaction and all its splits
    /// </summary>
    public async Task<bool> DeleteTransactionAsync(
        int transactionId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting transaction deletion.");

        var transaction = await _db.Transactions
            .Include(t => t.TransactionSplits)
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.Account.UserId == userId, cancellationToken);

        if (transaction is null)
            return false;

        // Remove splits first to be explicit regardless of cascade settings
        if (transaction.TransactionSplits?.Count > 0)
        {
            _db.TransactionSplits.RemoveRange(transaction.TransactionSplits);
        }

        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Verifies that a transaction belongs to a user
    /// </summary>
    public async Task<bool> TransactionBelongsToUserAsync(
        int transactionId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Transactions
            .AnyAsync(t => t.Id == transactionId && t.Account.UserId == userId, cancellationToken);
    }
}
