using Microsoft.EntityFrameworkCore;
using WNAB.Core.Models;
using WNAB.Core.Services;
using WNAB.Data;

namespace WNAB.API.Services;

public class AccountService : IAccountService
{
    private readonly WnabDbContext _context;

    public AccountService(WnabDbContext context)
    {
        _context = context;
    }

    public async Task<Account> CreateAccountAsync(int userId, CreateAccountRequest request)
    {
        var account = new Account
        {
            Name = request.Name,
            Type = request.Type,
            Balance = request.Balance,
            Description = request.Description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return account;
    }

    public async Task<Account?> GetAccountAsync(int userId, int accountId)
    {
        return await _context.Accounts
            .Include(a => a.Transactions.OrderByDescending(t => t.Date).Take(10))
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);
    }

    public async Task<IEnumerable<Account>> GetAccountsAsync(int userId)
    {
        return await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Account?> UpdateAccountAsync(int userId, int accountId, UpdateAccountRequest request)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        if (account == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            account.Name = request.Name;

        if (request.Type.HasValue)
            account.Type = request.Type.Value;

        if (request.Balance.HasValue)
            account.Balance = request.Balance.Value;

        if (request.Description != null)
            account.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;

        if (request.IsActive.HasValue)
            account.IsActive = request.IsActive.Value;

        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return account;
    }

    public async Task<bool> DeleteAccountAsync(int userId, int accountId)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        if (account == null)
        {
            return false;
        }

        // Check if account has transactions
        var hasTransactions = await _context.Transactions
            .AnyAsync(t => t.AccountId == accountId);

        if (hasTransactions)
        {
            // Soft delete - just mark as inactive
            account.IsActive = false;
            account.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Hard delete if no transactions
            _context.Accounts.Remove(account);
        }

        await _context.SaveChangesAsync();

        return true;
    }
}