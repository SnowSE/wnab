using System.Runtime.InteropServices;

namespace WNAB.Logic.Data;

// Request DTOs for POST endpoints
public record UserRecord(string FirstName, string LastName, string Email);
public record CategoryRecord(string Name, int UserId);
public record AccountRecord(string Name, int UserId); 
public record CategoryAllocationRecord(int CategoryId, decimal BudgetedAmount, int Month, int Year);
public record TransactionRecord(int AccountId, string Payee, decimal Amount, DateTime TransactionDate);
public record TransactionSplitRecord(int CategoryId, int TransactionId, decimal Amount);

// LLM-Dev:v1 Response DTOs to avoid circular references in API responses
public record TransactionDto(
    int Id,
    int AccountId,
    string AccountName,
    string Payee,
    string Description,
    decimal Amount,
    DateTime TransactionDate,
    bool IsReconciled,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<TransactionSplitDto> TransactionSplits
);

public record TransactionSplitDto(
    int Id,
    int CategoryId,
    string CategoryName,
    decimal Amount,
    string? Notes
);
