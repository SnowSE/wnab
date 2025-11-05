using Microsoft.EntityFrameworkCore;
using WNAB.Data;

namespace WNAB.API;

public class AccountDBService
{
    private readonly WnabContext _db;

    public AccountDBService(WnabContext db)
    {
        _db = db;
    }

    public async Task<Account> CreateAccountAsync(User user, string name, AccountType accountType = AccountType.Checking, CancellationToken cancellationToken = default)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Account name is required", nameof(name));

        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting account creation.");

        // Check for existing account with the same name (both active and inactive)
        var existingAccount = await _db.Accounts
            .FirstOrDefaultAsync(a => a.UserId == user.Id && a.AccountName == name, cancellationToken);

        if (existingAccount is not null)
        {
            // If account exists and is active, throw an error
            if (existingAccount.IsActive)
                throw new InvalidOperationException($"An active account with the name '{name}' already exists for this user.");

            // If account exists but is inactive, reactivate it
            existingAccount.IsActive = true;
            existingAccount.AccountType = accountType;
            existingAccount.UpdatedAt = DateTime.UtcNow;

            var reactivated = await _db.SaveChangesAsync(cancellationToken);
            
            // Postcondition: ensure only the account was updated
            if (reactivated != 1)
                throw new InvalidOperationException($"Expected to update exactly 1 entry, but updated {reactivated}.");

            return existingAccount;
        }

        // No existing account found, create a new one
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
            .Where(a => a.UserId == userId && a.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AccountBelongsToUserAsync(int accountId, int userId, CancellationToken cancellationToken = default)
    {
        return _db.Accounts.AnyAsync(a => a.Id == accountId && a.UserId == userId && a.IsActive, cancellationToken);
    }

    public async Task<Account?> UpdateAccountAsync(int accountId, int userId, string newName, AccountType newAccountType, int? requestAccountId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Account name is required", nameof(newName));

        // Validate that the route ID matches the request body ID (if provided)
        if (requestAccountId.HasValue && accountId != requestAccountId.Value)
            throw new ArgumentException($"Account ID mismatch: route ID {accountId} does not match request body ID {requestAccountId.Value}.", nameof(requestAccountId));

        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting account update.");

        // Verify the account exists and belongs to the user
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId && a.IsActive, cancellationToken);

        if (account is null)
            return null; // Account not found or doesn't belong to user

        // Check for duplicate account names for this user (excluding the current account, only check active accounts)
        var duplicateExists = await _db.Accounts
            .AnyAsync(a => a.UserId == userId && a.Id != accountId && a.AccountName == newName && a.IsActive, cancellationToken);

        if (duplicateExists)
            throw new InvalidOperationException($"An account with the name '{newName}' already exists for this user.");

        // Update the account
        account.AccountName = newName;
        account.AccountType = newAccountType;
        account.UpdatedAt = DateTime.UtcNow;

        var affected = await _db.SaveChangesAsync(cancellationToken);

        // Postcondition: ensure only the account was updated
        if (affected != 1)
            throw new InvalidOperationException($"Expected to update exactly 1 entry, but updated {affected}.");

        return account;
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAccountAsync(int accountId, int userId, CancellationToken cancellationToken = default)
    {
        // Validate account ID
        if (accountId <= 0)
            return (false, "Invalid account ID.");

        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting account deletion.");

        // Verify the account exists and belongs to the user
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive, cancellationToken);

        if (account is null)
            return (false, "Account not found.");

        if (account.UserId != userId)
            return (false, "Account does not belong to the current user.");

        // Soft delete: set IsActive to false instead of removing the account
        account.IsActive = false;
        account.UpdatedAt = DateTime.UtcNow;
        var affected = await _db.SaveChangesAsync(cancellationToken);

        // Postcondition: ensure only the account was updated
        if (affected != 1)
            throw new InvalidOperationException($"Expected to update exactly 1 entry, but updated {affected}.");

        return (true, null);
    }

    public async Task<List<Account>> GetInactiveAccountsForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _db.Accounts
            .Where(a => a.UserId == userId && !a.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string? ErrorMessage)> ReactivateAccountAsync(int accountId, int userId, CancellationToken cancellationToken = default)
    {
        // Validate account ID
        if (accountId <= 0)
            return (false, "Invalid account ID.");

        // Guard: prevent saving unrelated pending changes in this context
        if (_db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Context has pending changes; aborting account reactivation.");

        // Verify the account exists and belongs to the user
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && !a.IsActive, cancellationToken);

        if (account is null)
            return (false, "Inactive account not found.");

        if (account.UserId != userId)
            return (false, "Account does not belong to the current user.");

        // Check for duplicate active account names for this user
        var duplicateExists = await _db.Accounts
            .AnyAsync(a => a.UserId == userId && a.Id != accountId && a.AccountName == account.AccountName && a.IsActive, cancellationToken);

        if (duplicateExists)
            return (false, $"An active account with the name '{account.AccountName}' already exists. Please rename it first.");

        // Reactivate: set IsActive to true
        account.IsActive = true;
        account.UpdatedAt = DateTime.UtcNow;
        var affected = await _db.SaveChangesAsync(cancellationToken);

        // Postcondition: ensure only the account was updated
        if (affected != 1)
            throw new InvalidOperationException($"Expected to update exactly 1 entry, but updated {affected}.");

        return (true, null);
    }
}
