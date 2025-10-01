using WNAB.Logic;
using WNAB.Logic.Data;

namespace WNAB.Tests.Unit
{
    public partial class StepDefinitions
    {
        // LLM-Dev v6: Pure AAA pattern - no if/else logic in tests
        // Each step does exactly one thing: arrange data, act on service, or assert results
        // "When I enter transaction" always creates record (may be replaced by splits step)
        // "When I enter splits" replaces the record with multi-split version

        [Given("the following transaction")]
        public void GivenTheFollowingTransaction(DataTable dataTable)
        {
            // Arrange: Store raw transaction data from feature file
            var row = dataTable.Rows[0];
            context["TransactionDate"] = DateTime.Parse(row["Date"]);
            context["TransactionPayee"] = row["Payee"];
            if (dataTable.Header.Contains("Category"))
                context["TransactionCategory"] = row["Category"];
            context["TransactionMemo"] = row["Memo"];
            context["TransactionAmount"] = decimal.Parse(row["Amount"]);
        }

        [When("I enter the transaction")]
        public void WhenIEnterTheTransaction()
        {
            // Arrange: Get transaction data from context
            var date = context.Get<DateTime>("TransactionDate");
            var payee = context.Get<string>("TransactionPayee");
            var categoryName = context.Get<string>("TransactionCategory");
            var memo = context.Get<string>("TransactionMemo");
            var amount = context.Get<decimal>("TransactionAmount");
            
            var user = context.Get<User>("User");
            var catKey = $"Categories:{user.Email.ToLower()}";
            var categories = context.Get<List<Category>>(catKey);
            var category = categories.Single(c => c.Name == categoryName);
            
            var accountKey = $"Accounts:{user.Email.ToLower()}";
            var accounts = context.Get<List<Account>>(accountKey);
            var account = accounts.First();
            
            // Act: Create transaction record with single split using service
            var record = TransactionManagementService.CreateSimpleTransactionRecord(
                account.Id,
                payee,
                memo,
                amount,
                date,
                category.Id
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

        [When("I enter the transaction splits")]
        public void WhenIEnterTheTransactionSplits(DataTable dataTable)
        {
            // Arrange: Get transaction data and test entities from context
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
            
            // Act: Create transaction record with multiple splits using service (replaces single-split version)
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
