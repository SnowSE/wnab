using WNAB.Logic;
using WNAB.Logic.Data;

namespace WNAB.Tests.Unit
{
    public partial class StepDefinitions
    {
        // LLM-Dev v7: Removed Category from transaction - it doesn't exist on the Transaction entity
        // Category belongs to TransactionSplit, not Transaction
        // All transactions are created with splits (1 or more)

        [Given("the following transaction")]
        public void GivenTheFollowingTransaction(DataTable dataTable)
        {
            // Arrange: Store raw transaction data from feature file
            var row = dataTable.Rows[0];
            context["TransactionDate"] = DateTime.Parse(row["Date"]);
            context["TransactionPayee"] = row["Payee"];
            context["TransactionMemo"] = row["Memo"];
            context["TransactionAmount"] = decimal.Parse(row["Amount"]);
        }

        [When("I enter the transaction with split")]
        public void WhenIEnterTheTransactionWithSplit(DataTable dataTable)
        {
            // Arrange: Get transaction data from context
            var date = context.Get<DateTime>("TransactionDate");
            var payee = context.Get<string>("TransactionPayee");
            var memo = context.Get<string>("TransactionMemo");
            var amount = context.Get<decimal>("TransactionAmount");
            
            var user = context.Get<User>("User");
            var catKey = $"Categories:{user.Email.ToLower()}";
            var categories = context.Get<List<Category>>(catKey);
            var accountKey = $"Accounts:{user.Email.ToLower()}";
            var accounts = context.Get<List<Account>>(accountKey);
            var account = accounts.First();
            
            // Arrange: Build split records from feature file data
            var splitRecords = new List<TransactionSplitRecord>();
            foreach (var row in dataTable.Rows)
            {
                var categoryName = row["Category"].ToString();
                var splitAmount = decimal.Parse(row["Amount"].ToString());
                var category = categories.Single(c => c.Name == categoryName);
                
                splitRecords.Add(new TransactionSplitRecord(category.Id, splitAmount, null));
            }
            
            // Act: Create transaction record with splits using service
            var record = TransactionManagementService.CreateTransactionRecord(
                account.Id,
                payee,
                memo,
                amount,
                date,
                splitRecords
            );
            context["TransactionRecord"] = record;
        }

        [Then("I should have the following transaction entry")]
        public void ThenIShouldHaveTheFollowingTransactionEntry(DataTable dataTable)
        {
            // Arrange: Get expected values from feature file
            var row = dataTable.Rows[0];
            var expectedDate = DateTime.Parse(row["TransactionDate"]);
            var expectedAmount = decimal.Parse(row["Amount"]);
            var expectedMemo = row["Memo"];
            
            var actualRecord = context.Get<TransactionRecord>("TransactionRecord");
            
            // Assert: Verify transaction properties match expected values
            actualRecord.TransactionDate.ShouldBe(expectedDate);
            actualRecord.Amount.ShouldBe(expectedAmount);
            actualRecord.Description.ShouldBe(expectedMemo);
        }

        [Then("I should have the following transaction splits")]
        public void ThenIShouldHaveTheFollowingTransactionSplits(DataTable dataTable)
        {
            // Arrange: Get actual record and expected data from context and feature file
            // LLM-Dev v4: Validate TransactionRecord splits against feature file data
            var actualRecord = context.Get<TransactionRecord>("TransactionRecord");
            var user = context.Get<User>("User");
            var catKey = $"Categories:{user.Email.ToLower()}";
            var categories = context.Get<List<Category>>(catKey);
            
            var expectedSplits = dataTable.Rows.Select(row => new
            {
                Category = row["Category"].ToString(),
                Amount = decimal.Parse(row["Amount"].ToString())
            }).ToList();
            
            // Assert: Verify splits match expected values
            actualRecord.Splits.Count.ShouldBe(expectedSplits.Count);
            
            // Assert: Compare each split
            for (int i = 0; i < expectedSplits.Count; i++)
            {
                var expectedSplit = expectedSplits[i];
                var actualSplit = actualRecord.Splits[i];
                var expectedCategory = categories.Single(c => c.Name == expectedSplit.Category);
                
                actualSplit.Amount.ShouldBe(expectedSplit.Amount);
                actualSplit.CategoryId.ShouldBe(expectedCategory.Id);
            }
        }
    }
}
