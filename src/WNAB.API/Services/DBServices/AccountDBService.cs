using Microsoft.EntityFrameworkCore;
using WNAB.Data;

namespace WNAB.Services;

public class AccountDBService
{
    private readonly WnabContext _db;

    public AccountDBService(WnabContext db)
    {
        _db = db;
    }

    public async Task<Account> CreateAccountAsync(User user, string name, string accountType = "bank", CancellationToken cancellationToken = default)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Account name is required", nameof(name));

        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting account creation.");

        // Minimal parity with existing endpoint behavior
        var account = new Account
        {
            UserId = user.Id,
            AccountName = name,
            AccountType = accountType,
            User = user
        };

        _db.Accounts.Add(account);
        var affected = await _db.SaveChangesAsync(cancellationToken);

        // Postcondition: ensure only the account was saved
        if (affected != 1)
            throw new InvalidOperationException($"Expected to save exactly 1 entry, but saved {affected}.");

        return account;
    }

    public async Task<List<Account>> GetAccountsForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _db.Accounts
            .Where(a => a.UserId == userId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AccountBelongsToUserAsync(int accountId, int userId, CancellationToken cancellationToken = default)
    {
        return _db.Accounts.AnyAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);
    }
}
