using WNAB.Logic;
using WNAB.Logic.Data;

namespace WNAB.Tests.Unit
{
    public partial class StepDefinitions
    {
        // LLM-Dev v4.3: Use user.Categories instead of per-user context keys

        [Given("the following transaction")]
        public void GivenTheFollowingTransaction(DataTable dataTable)
        {
            // Inputs (expected)
            var row = dataTable.Rows[0];
            // Store
            context["TransactionDate"] = DateTime.Parse(row["Date"]);
            context["TransactionPayee"] = row["Payee"];
            context["TransactionMemo"] = row["Memo"];
            context["TransactionAmount"] = decimal.Parse(row["Amount"]);
        }

        [When("I enter the transaction with split")]
        public void WhenIEnterTheTransactionWithSplit(DataTable dataTable)
        {
            // Inputs (expected): build split items from the table
            var splitItems = new List<(string Category, decimal Amount)>();
            foreach (var row in dataTable.Rows)
            {
                splitItems.Add((row["Category"].ToString(), decimal.Parse(row["Amount"].ToString())));
            }

            // Actual: get transaction details and context objects
            var date = context.Get<DateTime>("TransactionDate");
            var payee = context.Get<string>("TransactionPayee");
            var memo = context.Get<string>("TransactionMemo");
            var amount = context.Get<decimal>("TransactionAmount");

            var user = context.Get<User>("User");
            var categories = user.Categories.ToList();
            var accounts = context.ContainsKey("Accounts")
                ? context.Get<List<Account>>("Accounts")
                : context.Get<List<Account>>($"Accounts:{user.Email.ToLower()}");
            var account = accounts.First();

            // LLM-Dev:v6 Budget-first enforcement: Allocations MUST exist before creating transactions
            var allocations = context.ContainsKey("Allocations") 
                ? context.Get<List<CategoryAllocation>>("Allocations") 
                : new List<CategoryAllocation>();

            // Act: map inputs to DTOs and create the record
            var splitRecords = new List<TransactionSplitRecord>();
            foreach (var (categoryName, splitAmount) in splitItems)
            {
                var category = categories.Single(c => c.Name == categoryName);
                if (category.Id == 0) category.Id = categories.IndexOf(category) + 1;
                
                // LLM-Dev:v6 ENFORCE budget-first: allocation MUST exist, do NOT auto-create
                var allocation = allocations.FirstOrDefault(a => 
                    a.CategoryId == category.Id && 
                    a.Month == date.Month && 
                    a.Year == date.Year);
                
                if (allocation == null)
                {
                    throw new InvalidOperationException(
                        $"No budget allocation found for category '{categoryName}' in {date:MMMM yyyy}. " +
                        "Budget allocations must be created before transactions (budget-first approach).");
                }
                
                splitRecords.Add(new TransactionSplitRecord(allocation.Id, splitAmount, false, null));
            }

            var record = TransactionManagementService.CreateTransactionRecord(
                account.Id,
                payee,
                memo,
                amount,
                date,
                splitRecords
            );

            // Store
            context["TransactionRecord"] = record;
        }

        // LLM-Dev:v6 Negative test step: attempt to create transaction and expect it to fail
        [When("I attempt to enter the transaction with split")]
        public void WhenIAttemptToEnterTheTransactionWithSplit(DataTable dataTable)
        {
            try
            {
                // Try to create the transaction (should fail)
                WhenIEnterTheTransactionWithSplit(dataTable);
                // If we get here, the test should fail because we expected an exception
                context["TransactionError"] = "No error occurred";
            }
            catch (Exception ex)
            {
                // Store the exception for verification
                context["TransactionError"] = ex.Message;
            }
        }

        [Then("the transaction creation should fail with message {string}")]
        public void ThenTheTransactionCreationShouldFailWithMessage(string expectedMessagePart)
        {
            var errorMessage = context.Get<string>("TransactionError");
            errorMessage.ShouldNotBe("No error occurred", "Expected an error but none occurred");
            errorMessage.ShouldContain(expectedMessagePart);
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
            var allocations = context.Get<List<CategoryAllocation>>("Allocations");

            // Assert
            actualRecord.Splits.Count.ShouldBe(expectedSplits.Count);
            for (int i = 0; i < expectedSplits.Count; i++)
            {
                var expectedSplit = expectedSplits[i];
                var actualSplit = actualRecord.Splits[i];
                var expectedCategory = categories.Single(c => c.Name == expectedSplit.Category);
                var expectedAllocation = allocations.Single(a => a.CategoryId == expectedCategory.Id);

                actualSplit.Amount.ShouldBe(expectedSplit.Amount);
                actualSplit.CategoryAllocationId.ShouldBe(expectedAllocation.Id);
            }
        }
    }
}
