using System.Runtime.InteropServices;

namespace WNAB.Logic.Data;

// Request DTOs for POST endpoints
public record UserRecord(string FirstName, string LastName, string Email);
public record CategoryRecord(string Name, int UserId);
public record AccountRecord(string Name, int UserId); // LLM-Dev: UserId in record for database, also passed as separate parameter in API
public record CategoryAllocationRecord(int CategoryId, decimal BudgetedAmount, int Month, int Year);
public record TransactionRecord(int AccountId, string Payee, decimal Amount, DateTime TransactionDate);
public record TransactionSplitRecord(int CategoryId, int TransactionId, decimal Amount); // LLM-Dev:v2 Fixed parameter name from TransactionRecordId to TransactionId to match API
