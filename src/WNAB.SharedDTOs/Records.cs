namespace WNAB.SharedDTOs;

// Request DTOs for POST endpoints
public record UserRecord(string FirstName, string LastName, string Email);
public record CategoryRecord(string Name);
public record AccountRecord(string Name);
public record EditAccountRequest(int Id, string NewName, string NewAccountType);
public record CategoryAllocationRecord(int CategoryId, decimal BudgetedAmount, int Month, int Year, string? EditorName = null, decimal? PercentageAllocation = null, decimal? OldAmount = null, string? EditedMemo = null);

// Transaction creation with splits
public record TransactionRecord(
    int AccountId,
    string Payee,
    string Description,
    decimal Amount,
    DateTime TransactionDate,
    List<TransactionSplitRecord> Splits
);

public record TransactionSplitRecord(
    int CategoryAllocationId,
    int TransactionId,
    decimal Amount,
    bool IsIncome,
 string? Notes
);

// Create the transaction. 
public record CreateTransactionRequest(
    string Name, 
    string Payee, 
    decimal Amount, 
    string Description, 
    DateTime TransactionDate
);

// Create a split.
public record CreateTransactionSplitRequest(
    int TransactionId, 
    string Name, 
    decimal Amount, 
    string Description, 
    DateTime TransactionDate
);

// get the transactions
public record GetTransactionsResponse(
    List<TransactionResponse> Transactions
);

// get the splits
public record GetTransactionSplitsResponse(
    List<TransactionSplitResponse> TransactionSplits
);

public record TransactionResponse(
    int Id,
    int AccountId,
    string AccountName,
    string Payee,
    string Description,
    decimal Amount,
    DateTime TransactionDate,
    bool IsReconciled,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TransactionSplitResponse(
    int Id,
    int CategoryAllocationId,
    int TransactionId,
    string CategoryName,
    decimal Amount,
    bool IsIncome,
    string? Description
);



/// <summary>
/// DTO for creating a new category - no circular references
/// </summary>
public record CategoryCreateDto(string Name);

/// <summary>
/// DTO for returning category data - no User navigation property
/// </summary>
public record CategoryDto(
    int Id,
    string Name,
    string? Description,
    string? Color,
    bool IsIncome,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
