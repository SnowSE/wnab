namespace WNAB.Logic.Data;

// Request DTOs for POST endpoints
public record UserRecord(string FirstName, string LastName, string Email);
public record CategoryRecord(string Name, int UserId);
public record AccountRecord(string Name);
public record CategoryAllocationRecord(int CategoryId, decimal BudgetedAmount, int Month, int Year);
public record TransactionRecord(int AccountId, string Payee, string Description, decimal Amount, DateTime TransactionDate, List<TransactionSplitRecord> Splits);
public record TransactionSplitRecord(int CategoryId, decimal Amount, string? Notes);
