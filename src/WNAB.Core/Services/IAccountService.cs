using WNAB.Core.Models;

namespace WNAB.Core.Services;

public interface IAccountService
{
    Task<Account> CreateAccountAsync(int userId, CreateAccountRequest request);
    Task<Account?> GetAccountAsync(int userId, int accountId);
    Task<IEnumerable<Account>> GetAccountsAsync(int userId);
    Task<Account?> UpdateAccountAsync(int userId, int accountId, UpdateAccountRequest request);
    Task<bool> DeleteAccountAsync(int userId, int accountId);
}

public class CreateAccountRequest
{
    public required string Name { get; set; }
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string? Description { get; set; }
}

public class UpdateAccountRequest
{
    public string? Name { get; set; }
    public AccountType? Type { get; set; }
    public decimal? Balance { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}