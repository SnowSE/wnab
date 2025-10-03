using WNAB.Logic;
using WNAB.Logic.Data;

namespace WNAB.Tests.Unit
{
    public partial class StepDefinitions
    {
        // LLM-Dev v6.1: Create proper TransactionRecord in Given step (with empty splits), TransactionSplitRecords in separate Given step, combine using service in When step
        private readonly TransactionManagementService _transactionService = new(new HttpClient());

        [Given("the following transaction")]
        public void GivenTheFollowingTransaction(DataTable dataTable)
        {
            // Inputs: parse transaction data from table
            var row = dataTable.Rows[0];
            var date = DateTime.Parse(row["Date"]);
            var payee = row["Payee"];
            var memo = row["Memo"];
            var amount = decimal.Parse(row["Amount"]);
            
            // Get account info from context (should be set up in earlier steps)
            var user = context.Get<User>("User");
            var accounts = context.ContainsKey("Accounts")
                ? context.Get<List<Account>>("Accounts")
                : context.Get<List<Account>>($"Accounts:{user.Email.ToLower()}");
            var account = accounts.First();
            
            // Store transaction record for later use (with empty splits initially)
            var transactionRecord = TransactionManagementService.CreateTransactionRecord(
                account.Id,
                payee,
                memo,
                amount,
                date,
                new List<TransactionSplitRecord>() // Empty splits initially
            );
            
            // Store the transaction record
            context["TransactionRecord"] = transactionRecord;
        }

        [Given("the following transaction splits")]
        public void GivenTheFollowingTransactionSplits(DataTable dataTable)
        {
            // Get user and categories from context
            var user = context.Get<User>("User");
            var categories = user.Categories.ToList();
            
            // Create TransactionSplitRecord objects from the table using the service method
            var splitRecords = new List<TransactionSplitRecord>();
            foreach (var row in dataTable.Rows)
            {
                var categoryName = row["Category"].ToString();
                var amount = decimal.Parse(row["Amount"].ToString());
                var category = categories.Single(c => c.Name == categoryName);
                
                // Create split record using service method
                splitRecords.Add(TransactionManagementService.CreateTransactionSplitRecord(category.Id, amount, null));
            }
            
            // Store the split records
            context["TransactionSplitRecords"] = splitRecords;
        }

        [When("I enter the transaction with split")]
        public void WhenIEnterTheTransactionWithSplit()
        {
            // Get the pre-created transaction record and split records from context
            var transactionRecord = context.Get<TransactionRecord>("TransactionRecord");
            var splitRecords = context.Get<List<TransactionSplitRecord>>("TransactionSplitRecords");
            
            // Create the final TransactionRecord with the splits using the service static method
            var finalRecord = TransactionManagementService.CreateTransactionRecord(
                transactionRecord.AccountId,
                transactionRecord.Payee,
                transactionRecord.Description,
                transactionRecord.Amount,
                transactionRecord.TransactionDate,
                splitRecords
            );
            
            // Store the final complete record
            context["TransactionRecord"] = finalRecord;
        }

        [Then("I should have the following transaction entry")]
        public void ThenIShouldHaveTheFollowingTransactionEntry(DataTable dataTable)
        {
            // Inputs (expected)
            var row = dataTable.Rows[0];
            var expectedDate = DateTime.Parse(row["TransactionDate"]);
            var expectedAmount = decimal.Parse(row["Amount"]);
            var expectedMemo = row["Memo"];

            // Actual
            var actualRecord = context.Get<TransactionRecord>("TransactionRecord");

            // Assert
            actualRecord.TransactionDate.ShouldBe(expectedDate);
            actualRecord.Amount.ShouldBe(expectedAmount);
            actualRecord.Description.ShouldBe(expectedMemo);
        }

        [Then("I should have the following transaction splits")]
        public void ThenIShouldHaveTheFollowingTransactionSplits(DataTable dataTable)
        {
            // Inputs (expected)
            var expectedSplits = dataTable.Rows.Select(row => new
            {
                Category = row["Category"].ToString(),
                Amount = decimal.Parse(row["Amount"].ToString())
            }).ToList();

            // Actual
            var actualRecord = context.Get<TransactionRecord>("TransactionRecord");
            var user = context.Get<User>("User");
            var categories = user.Categories.ToList();

            // Assert
            actualRecord.Splits.Count.ShouldBe(expectedSplits.Count);
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
