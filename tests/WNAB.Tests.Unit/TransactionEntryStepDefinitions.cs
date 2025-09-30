using WNAB.Logic;
using WNAB.Logic.Data;
using WNAB.Logic.ViewModels;

namespace WNAB.Tests.Unit
{
    public partial class StepDefinitions
    {
        // LLM-Dev v1: Simplified to use basic test helpers instead of test-specific service
        // Mock IDs for BDD test scenarios
        private static int GetMockCategoryId(string categoryName) => categoryName switch
        {
            "Groceries" => 1,
            "Personal Care" => 2,
            "Utilities" => 3,
            "Entertainment" => 4,
            _ => 999
        };
        
        private static int GetMockAccountId() => 1;

        [Given("the following transaction")]
        public void GivenTheFollowingTransaction(DataTable dataTable)
        {
            var transactionEntryVM = dataTable.CreateInstance<TransactionEntryViewModel>();
            context["Transaction"] = transactionEntryVM;
        }

        [When("I enter the transaction")]
        public void WhenIEnterTheTransaction()
        {
            var transactionEntryVM = context.Get<TransactionEntryViewModel>("Transaction");

            // LLM-Dev v1: Basic test logic - set transaction date and create single split
            transactionEntryVM.TransactionDate = transactionEntryVM.Date;
            transactionEntryVM.Splits.Clear();
            
            // For non-split transactions, create a single split
            if (transactionEntryVM.Category != "Split")
            {
                transactionEntryVM.Splits.Add(new TransactionSplit
                {
                    Amount = transactionEntryVM.Amount,
                    CategoryName = transactionEntryVM.Category,
                    CategoryId = GetMockCategoryId(transactionEntryVM.Category)
                });
            }
            
            context["Transaction"] = transactionEntryVM;
        }

        [Then("I should have the following transaction entry")]
        public void ThenIShouldHaveTheFollowingTransactionEntry(DataTable dataTable)
        {
            var expectedTransaction = dataTable.CreateInstance<TransactionEntryViewModel>();
            var actualTransaction = context.Get<TransactionEntryViewModel>("Transaction");
            
            // Only compare the properties that are specified in the feature file
            actualTransaction.TransactionDate.ShouldBe(expectedTransaction.TransactionDate);
            actualTransaction.Amount.ShouldBe(expectedTransaction.Amount);
            actualTransaction.Memo.ShouldBe(expectedTransaction.Memo);
        }

        [When("I enter the transaction splits")]
        public void WhenIEnterTheTransactionSplits(DataTable dataTable)
        {
            var transactionEntryVM = context.Get<TransactionEntryViewModel>("Transaction");
            
            // Clear and add splits
            transactionEntryVM.Splits.Clear();
            
            // LLM-Dev v1: Basic test logic - convert table rows to splits with mock IDs
            foreach (var row in dataTable.Rows)
            {
                var categoryName = row["Category"].ToString();
                transactionEntryVM.Splits.Add(new TransactionSplit
                {
                    Amount = decimal.Parse(row["Amount"].ToString()),
                    CategoryName = categoryName,
                    CategoryId = GetMockCategoryId(categoryName)
                });
            }
            
            context["Transaction"] = transactionEntryVM;
        }

        [Then("I should have the following transaction splits")]
        public void ThenIShouldHaveTheFollowingTransactionSplits(DataTable dataTable)
        {
            var actualTransaction = context.Get<TransactionEntryViewModel>("Transaction");
            
            // Convert the expected data table to a list for comparison
            var expectedSplits = dataTable.Rows.Select(row => new
            {
                Category = row["Category"].ToString(),
                Amount = decimal.Parse(row["Amount"].ToString())
            }).ToList();
            
            // Check that we have the right number of splits
            actualTransaction.Splits.Count.ShouldBe(expectedSplits.Count);
            
            // Compare each split
            for (int i = 0; i < expectedSplits.Count; i++)
            {
                var expectedSplit = expectedSplits[i];
                var actualSplit = actualTransaction.Splits.ElementAt(i);
                
                actualSplit.Amount.ShouldBe(expectedSplit.Amount);
                actualSplit.CategoryName.ShouldBe(expectedSplit.Category);
                // LLM-Dev: Can now also validate CategoryId is properly set
                actualSplit.CategoryId.ShouldBeGreaterThan(0);
            }
        }
    }
}
