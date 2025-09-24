using WNAB.Logic.Data;
using WNAB.Logic.ViewModels;
using WNAB.Logic.Interfaces;

namespace WNAB.Logic;

public class TransactionEntryService : ITransactionEntryService
{
    // LLM-Dev: Minimal implementation to satisfy test compilation
    public TransactionEntryViewModel AddTransaction(TransactionEntryViewModel transactionEntryVM)
    {
        // Set the transaction date
        transactionEntryVM.TransactionDate = transactionEntryVM.Date;
        
        // Clear any existing splits
        transactionEntryVM.Splits.Clear();
        
        // For non-split transactions, create a single split with the transaction category
        if (transactionEntryVM.Category != "Split")
        {
            // Single category transaction - create one split with the full amount
            transactionEntryVM.Splits.Add(new TransactionSplit 
            { 
                Amount = transactionEntryVM.Amount,
                CategoryName = transactionEntryVM.Category  // LLM-Dev: Set CategoryName for BDD test compatibility
            });
        }
        // For "Split" category transactions, splits will be added separately
        
        return transactionEntryVM;
    }

    public TransactionEntryViewModel AddTransactionSplits(TransactionEntryViewModel transactionEntryVM, IEnumerable<TransactionSplit> splits)
    {
        // Clear existing splits and add the new ones
        transactionEntryVM.Splits.Clear();
        
        foreach (var split in splits)
        {
            transactionEntryVM.Splits.Add(split);
        }
        
        return transactionEntryVM;
    }
}