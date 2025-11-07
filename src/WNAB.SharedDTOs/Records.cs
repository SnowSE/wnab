using WNAB.Data;

namespace WNAB.SharedDTOs;

// Categories
public record CategoryRecord(string Name);
public record CreateCategoryRequest(string Name, string Color);
public record EditCategoryRequest(int Id, string NewName, string NewColor, bool IsActive);
public record DeleteCategoryRequest(int Id);
public record CategoryResponse(int Id, string Name, string? Color, bool IsActive );

// Accounts
public record AccountRecord(string Name, AccountType AccountType = AccountType.Checking);
public record EditAccountRequest(int Id, string NewName, AccountType NewAccountType, bool IsActive);

// Category Allocations
public record CategoryAllocationRecord(int CategoryId, decimal BudgetedAmount, int Month, int Year, string? EditorName = null, decimal? PercentageAllocation = null, decimal? OldAmount = null, string? EditedMemo = null);
public record UpdateCategoryAllocationRequest(int Id, decimal? BudgetedAmount = null, bool? IsActive = null, string? EditorName = null, string? EditedMemo = null);
public record CategoryAllocationResponse(int Id, int CategoryId, decimal BudgetedAmount, int Month, int Year, string? EditorName, decimal? PercentageAllocation, decimal? OldAmount, string? EditedMemo, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt );

// Transactions
public record TransactionRecord(int AccountId, string Payee, string Description, decimal Amount, DateTime TransactionDate, List<TransactionSplitRecord> Splits );
public record TransactionSplitRecord(int CategoryAllocationId, int TransactionId, decimal Amount, bool IsIncome,  string? Notes );
public record EditTransactionRequest(int Id, int AccountId, string Payee, string Description, decimal Amount, DateTime TransactionDate, bool IsReconciled );
public record EditTransactionSplitRequest(int Id, int CategoryAllocationId, decimal Amount, bool IsIncome, string? Description );
public record CreateTransactionRequest(string Name, string Payee, decimal Amount, string Description, DateTime TransactionDate);
public record CreateTransactionSplitRequest(int TransactionId, string Name, decimal Amount, string Description, DateTime TransactionDate);
public record GetTransactionsResponse(List<TransactionResponse> Transactions);
public record GetTransactionSplitsResponse(List<TransactionSplitResponse> TransactionSplits);
public record TransactionResponse(int Id, int AccountId, string AccountName, string Payee, string Description, decimal Amount, DateTime TransactionDate, bool IsReconciled, DateTime CreatedAt, DateTime UpdatedAt );
public record TransactionSplitResponse(int Id, int CategoryAllocationId, int TransactionId, string CategoryName, decimal Amount, bool IsIncome, string? Description );
