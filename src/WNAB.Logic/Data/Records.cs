using System.Runtime.InteropServices;

namespace WNAB.Logic.Data;

// Request DTOs for POST endpoints
public record UserRecord(string FirstName, string LastName, string Email);
public record CategoryRecord(string Name, int UserId);
public record AccountRecord(string Name, int UserId);
public record CategoryAllocationRecord(int CategoryId, decimal BudgetedAmount, int Month, int Year);
public record TransactionRecord(int AccountId, string Payee, string Description, decimal Amount, DateTime TransactionDate);
public record TransactionSplitRecord(int CategoryId, int TransactionRecordId, decimal Amount);
