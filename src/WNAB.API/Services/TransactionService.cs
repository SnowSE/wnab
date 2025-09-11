using Microsoft.EntityFrameworkCore;
using WNAB.Core.Models;
using WNAB.Core.Services;
using WNAB.Data;

namespace WNAB.API.Services;

public class TransactionService : ITransactionService
{
    private readonly WnabDbContext _context;

    public TransactionService(WnabDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction> CreateTransactionAsync(int userId, CreateTransactionRequest request)
    {
        // Verify account belongs to user
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId);

        if (account == null)
        {
            throw new ArgumentException("Account not found or does not belong to user");
        }

        // Verify category belongs to user (if specified)
        if (request.CategoryId.HasValue)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == userId);

            if (category == null)
            {
                throw new ArgumentException("Category not found or does not belong to user");
            }
        }

        var transaction = new Transaction
        {
            Description = request.Description,
            Amount = request.Amount,
            Date = request.Date,
            Type = request.Type,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Notes = request.Notes,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);

        // Update account balance
        if (request.Type == TransactionType.Income)
        {
            account.Balance += request.Amount;
        }
        else if (request.Type == TransactionType.Expense)
        {
            account.Balance -= request.Amount;
        }

        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetTransactionAsync(userId, transaction.Id) ?? transaction;
    }

    public async Task<Transaction?> GetTransactionAsync(int userId, int transactionId)
    {
        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsAsync(int userId, TransactionFilter? filter = null)
    {
        var query = _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.UserId == userId);

        if (filter != null)
        {
            if (filter.AccountId.HasValue)
                query = query.Where(t => t.AccountId == filter.AccountId.Value);

            if (filter.CategoryId.HasValue)
                query = query.Where(t => t.CategoryId == filter.CategoryId.Value);

            if (filter.StartDate.HasValue)
                query = query.Where(t => t.Date >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(t => t.Date <= filter.EndDate.Value);

            if (filter.Type.HasValue)
                query = query.Where(t => t.Type == filter.Type.Value);

            if (filter.IsCleared.HasValue)
                query = query.Where(t => t.IsCleared == filter.IsCleared.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                query = query.Where(t => t.Description.Contains(filter.SearchTerm) || 
                                    (t.Notes != null && t.Notes.Contains(filter.SearchTerm)));
        }

        return await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip(filter?.Skip ?? 0)
            .Take(filter?.Take ?? 50)
            .ToListAsync();
    }

    public async Task<Transaction?> UpdateTransactionAsync(int userId, int transactionId, UpdateTransactionRequest request)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

        if (transaction == null)
        {
            return null;
        }

        var oldAmount = transaction.Amount;
        var oldType = transaction.Type;
        var oldAccountId = transaction.AccountId;

        // Update transaction properties
        if (!string.IsNullOrWhiteSpace(request.Description))
            transaction.Description = request.Description;

        if (request.Amount.HasValue)
            transaction.Amount = request.Amount.Value;

        if (request.Date.HasValue)
            transaction.Date = request.Date.Value;

        if (request.Type.HasValue)
            transaction.Type = request.Type.Value;

        if (request.AccountId.HasValue && request.AccountId.Value != transaction.AccountId)
        {
            // Verify new account belongs to user
            var newAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.AccountId.Value && a.UserId == userId);

            if (newAccount == null)
            {
                throw new ArgumentException("Account not found or does not belong to user");
            }

            transaction.AccountId = request.AccountId.Value;
        }

        if (request.CategoryId.HasValue)
        {
            if (request.CategoryId.Value > 0)
            {
                // Verify category belongs to user
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == userId);

                if (category == null)
                {
                    throw new ArgumentException("Category not found or does not belong to user");
                }
            }

            transaction.CategoryId = request.CategoryId.Value > 0 ? request.CategoryId.Value : null;
        }

        if (request.Notes != null)
            transaction.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes;

        if (request.IsCleared.HasValue)
            transaction.IsCleared = request.IsCleared.Value;

        transaction.UpdatedAt = DateTime.UtcNow;

        // Update account balances if amount, type, or account changed
        if (request.Amount.HasValue || request.Type.HasValue || request.AccountId.HasValue)
        {
            // Revert old transaction from old account
            if (oldType == TransactionType.Income)
            {
                transaction.Account.Balance -= oldAmount;
            }
            else if (oldType == TransactionType.Expense)
            {
                transaction.Account.Balance += oldAmount;
            }

            // If account changed, get the new account
            if (request.AccountId.HasValue && request.AccountId.Value != oldAccountId)
            {
                var newAccount = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.AccountId.Value && a.UserId == userId);
                
                if (newAccount != null)
                {
                    // Apply new transaction to new account
                    if (transaction.Type == TransactionType.Income)
                    {
                        newAccount.Balance += transaction.Amount;
                    }
                    else if (transaction.Type == TransactionType.Expense)
                    {
                        newAccount.Balance -= transaction.Amount;
                    }
                    newAccount.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // Apply new transaction to same account
                if (transaction.Type == TransactionType.Income)
                {
                    transaction.Account.Balance += transaction.Amount;
                }
                else if (transaction.Type == TransactionType.Expense)
                {
                    transaction.Account.Balance -= transaction.Amount;
                }
            }

            transaction.Account.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return await GetTransactionAsync(userId, transactionId);
    }

    public async Task<bool> DeleteTransactionAsync(int userId, int transactionId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

        if (transaction == null)
        {
            return false;
        }

        // Update account balance
        if (transaction.Type == TransactionType.Income)
        {
            transaction.Account.Balance -= transaction.Amount;
        }
        else if (transaction.Type == TransactionType.Expense)
        {
            transaction.Account.Balance += transaction.Amount;
        }

        transaction.Account.UpdatedAt = DateTime.UtcNow;

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();

        return true;
    }
}