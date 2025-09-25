using WNAB.Logic.ViewModels;
using WNAB.Logic.Data;

namespace WNAB.Logic.Interfaces;

public interface ITransactionEntryService
{
    TransactionEntryViewModel AddTransaction(TransactionEntryViewModel transactionEntryVM);
    TransactionEntryViewModel AddTransactionSplits(TransactionEntryViewModel transactionEntryVM, IEnumerable<TransactionSplit> splits);
}
