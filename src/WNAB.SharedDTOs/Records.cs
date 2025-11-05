using WNAB.Data;

namespace WNAB.SharedDTOs;

// Request DTOs for POST endpoints
public record UserRecord(string FirstName, string LastName, string Email);
public record CategoryRecord(string Name);
public record AccountRecord(string Name, AccountType AccountType = AccountType.Checking);
public record EditAccountRequest(int Id, string NewName, AccountType NewAccountType);
public record CategoryAllocationRecord(int CategoryId, decimal BudgetedAmount, int Month, int Year, string? EditorName = null, decimal? PercentageAllocation = null, decimal? OldAmount = null, string? EditedMemo = null);
public record UpdateCategoryAllocationRequest(int Id, decimal? BudgetedAmount = null, bool? IsActive = null, string? EditorName = null, string? EditedMemo = null);

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

// Edit/Update DTOs
public record EditTransactionRequest(
    int Id,
    int AccountId,
    string Payee,
    string Description,
    decimal Amount,
    DateTime TransactionDate,
    bool IsReconciled
);

public record EditTransactionSplitRequest(
    int Id,
    int CategoryAllocationId,
  decimal Amount,
    bool IsIncome,
    string? Description
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
/// DTO for returning category allocation data - no navigation properties
/// </summary>
public record CategoryAllocationDto(
    int Id,
    int CategoryId,
    decimal BudgetedAmount,
    int Month,
    int Year,
    string? EditorName,
    decimal? PercentageAllocation,
    decimal? OldAmount,
    string? EditedMemo,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO for creating a new category - no circular references
/// </summary>
public record CreateCategoryRequest(string Name, string Color);
public record EditCategoryRequest(int Id, string NewName, string NewColor, bool IsActive);
public record DeleteCategoryRequest(int Id);
/// <summary>
/// DTO for returning category data - no User navigation property
/// </summary>
public record CategoryDto(
    int Id,
    string Name,
    string? Color,
    bool IsActive
);
