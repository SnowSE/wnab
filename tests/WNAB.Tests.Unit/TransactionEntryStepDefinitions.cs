using WNAB.Logic;
using WNAB.Logic.Data;
using Shouldly;
using Reqnroll;

namespace WNAB.Tests.Unit
{
    public partial class StepDefinitions
    {
        // LLM-Dev v7.1: Updated to follow prescribed pattern: Given stores records, When converts to objects, Then compares objects
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
            
            // Store transaction record for later use (without splits - they'll be created separately)
            var transactionRecord = TransactionManagementService.CreateTransactionRecord(
                account.Id,
                payee,
                memo,
                amount,
                date
            );
            
            // Store the transaction record
            context["TransactionRecord"] = transactionRecord;
        }

        [Given("the following transaction splits")]
        public void GivenTheFollowingTransactionSplits(DataTable dataTable)
        {
            // Get user and categories from context
            var user = context.Get<User>("User");
            
            // LLM-Dev v1: Convert category records to objects if not already done (like in CategoryAllocationStepDefinitions)
            if (context.ContainsKey("CategoryRecords"))
            {
                var categoryRecords = context.Get<List<CategoryRecord>>("CategoryRecords");
                var convertedCategories = new List<Category>();
                int categoryId = 1;
                
                foreach (var record in categoryRecords)
                {
                    var category = new Category(record)
                    {
                        Id = categoryId++,
                        User = user
                    };
                    convertedCategories.Add(category);
                }
                
                user.Categories = convertedCategories;
                context.Remove("CategoryRecords"); // Remove records after conversion
            }
            
            var categories = user.Categories.ToList();
            
            // Create TransactionSplitRecord objects from the table using the service method
            var splitRecords = new List<TransactionSplitRecord>();
            foreach (var row in dataTable.Rows)
            {
                var categoryName = row["Category"].ToString();
                var amount = decimal.Parse(row["Amount"].ToString());
                var category = categories.Single(c => c.Name == categoryName);
                
                // Create split record using service method (we'll set transactionId later)
                // Note: using 1 as placeholder transactionId since we don't have the actual transaction ID yet
                splitRecords.Add(TransactionManagementService.CreateTransactionSplitRecord(category.Id, 1, amount));
            }
            
            // Store the split records
            context["TransactionSplitRecords"] = splitRecords;
        }

        [When("I enter the transaction with split")]
        public void WhenIEnterTheTransactionWithSplit()
        {
            // Actual: Get records from context
            var user = context.Get<User>("User");
            var transactionRecord = context.Get<TransactionRecord>("TransactionRecord");
            var splitRecords = context.Get<List<TransactionSplitRecord>>("TransactionSplitRecords");
            
            // Act: Convert records to objects
            var transaction = new Transaction(transactionRecord)
            {
                Id = 1, // LLM-Dev:v6.1 Set test ID for transaction
                Account = user.Accounts.First(a => a.Id == transactionRecord.AccountId),
                Payee = transactionRecord.Payee // LLM-Dev:v6.1 Explicitly set required property
            };
            
            var transactionSplits = splitRecords.Select((split, index) => new TransactionSplit(split)
            {
                Id = index + 1, // LLM-Dev:v6.1 Set test ID for splits
                TransactionId = transaction.Id,
                Transaction = transaction,
                Category = user.Categories.First(c => c.Id == split.CategoryId)
            }).ToList();
            
            transaction.TransactionSplits = transactionSplits;
            
            // Store: Store converted objects
            context["Transaction"] = transaction;
            context["TransactionSplits"] = transactionSplits;
        }

        [Then("I should have the following transaction entry")]
        public void ThenIShouldHaveTheFollowingTransactionEntry(DataTable dataTable)
        {
            // Inputs (expected)
            var row = dataTable.Rows[0];
            var expectedDate = DateTime.Parse(row["TransactionDate"]);
            var expectedAmount = decimal.Parse(row["Amount"]);
            var expectedMemo = row["Memo"];

            // Actual: Compare against converted object, not record
            var actual = context.Get<Transaction>("Transaction");

            // Assert
            actual.TransactionDate.ShouldBe(expectedDate);
            actual.Amount.ShouldBe(expectedAmount);
            actual.Description.ShouldBe(expectedMemo);
        }

        [Then("I should have the following transaction splits")]
        public void ThenIShouldHaveTheFollowingTransactionSplits(DataTable dataTable)
        {
            // LLM-Dev v6.2: Updated to work with converted transaction split objects
            // Inputs (expected)
            var expectedSplits = dataTable.Rows.Select(row => new
            {
                Category = row["Category"].ToString(),
                Amount = decimal.Parse(row["Amount"].ToString())
            }).ToList();

            // Actual - get splits from converted objects instead of records
            var actualSplits = context.Get<List<TransactionSplit>>("TransactionSplits");
            var user = context.Get<User>("User");
            var categories = user.Categories.ToList();

            // Assert
            actualSplits.Count.ShouldBe(expectedSplits.Count);
            for (int i = 0; i < expectedSplits.Count; i++)
            {
                var expectedSplit = expectedSplits[i];
                var actualSplit = actualSplits[i];
                var expectedCategory = categories.Single(c => c.Name == expectedSplit.Category);

                actualSplit.Amount.ShouldBe(expectedSplit.Amount);
                actualSplit.CategoryId.ShouldBe(expectedCategory.Id);
            }
        }
    }
}
