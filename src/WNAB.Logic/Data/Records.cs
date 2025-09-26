namespace WNAB.Logic.Data;

// Request DTOs for POST endpoints
public record UserRecord(string Name, string Email);
public record CategoryRecord(string Name, int UserId);
public record AccountRecord(string Name);
public record CategoryAllocationRecord(int CategoryId, decimal BudgetedAmount, int Month, int Year);
