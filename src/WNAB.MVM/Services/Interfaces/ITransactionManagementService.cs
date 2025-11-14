using WNAB.SharedDTOs;

namespace WNAB.MVM;

/// <summary>
/// Interface for transaction-related operations via the API.
/// </summary>
public interface ITransactionManagementService
{
    Task<int> CreateTransactionAsync(TransactionRecord record, CancellationToken ct = default);
    Task<List<TransactionResponse>> GetTransactionsForAccountAsync(int accountId, CancellationToken ct = default);
    Task<List<TransactionResponse>> GetTransactionsAsync(int? accountId = null, CancellationToken ct = default);
    Task<List<TransactionResponse>> GetTransactionsForUserAsync(CancellationToken ct = default);
    Task<TransactionResponse?> GetTransactionByIdAsync(int transactionId, CancellationToken ct = default);
    Task<TransactionResponse> UpdateTransactionAsync(EditTransactionRequest request, CancellationToken ct = default);
    Task<int> CreateTransactionSplitAsync(TransactionSplitRecord record, CancellationToken ct = default);
    Task<List<TransactionSplitResponse>> GetTransactionSplitsForAllocationAsync(int allocationId, CancellationToken ct = default);
    Task<List<TransactionSplitResponse>> GetTransactionSplitsAsync(CancellationToken ct = default);
    Task<TransactionSplitResponse?> GetTransactionSplitByIdAsync(int splitId, CancellationToken ct = default);
    Task<TransactionSplitResponse> UpdateTransactionSplitAsync(EditTransactionSplitRequest request, CancellationToken ct = default);
    Task DeleteTransactionAsync(int transactionId, CancellationToken ct = default);
    Task DeleteTransactionSplitAsync(int transactionSplitId, CancellationToken ct = default);
    Task<List<TransactionSplitResponse>> GetTransactionSplitsByMonthAsync(DateTime date, CancellationToken ct = default);
}
