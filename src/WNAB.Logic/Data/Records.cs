namespace WNAB.Logic.Data;

// Request DTOs for POST endpoints
public record UserRecord(string FirstName, string LastName, string Email);
public record CategoryRecord(string Name);
public record AccountRecord(string Name);
public record CategoryAllocationRecord(int CategoryId, decimal BudgetedAmount, int Month, int Year, string? EditorName = null, decimal? PercentageAllocation = null, decimal? OldAmount = null, string? EditedMemo = null);
public record TransactionRecord(int AccountId, string Payee, string Description, decimal Amount, DateTime TransactionDate, List<TransactionSplitRecord> Splits);
public record TransactionSplitRecord(int CategoryAllocationId, decimal Amount, bool IsIncome, string? Notes);

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
    int CategoryAllocationId,
    string CategoryName,
    decimal Amount,
    bool IsIncome,
    string? Notes
);
