using WNAB.Logic;
using WNAB.Logic.Data;
using Shouldly;
using Reqnroll;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
    // prescribed pattern: (Given) creates and stores records, (When) uses services to create objects, (Then) compares objects
    // Rule: Use the services where possible.
    // Rule: functions may only have datatable as a parameter or no parameter.

    [Given(@"the following transactions")]
    public void GivenTheFollowingTransactions(DataTable dataTable)
    {
        // Inputs: parse transaction data from table and create records
        var user = context.Get<User>("User");
        var account = user.Accounts.First(); // Use first available account
        
        var transactionRecords = context.ContainsKey("TransactionRecords") 
            ? context.Get<List<TransactionRecord>>("TransactionRecords") 
            : new List<TransactionRecord>();
        
        foreach (var row in dataTable.Rows)
        {
            var date = DateTime.Parse(row["Date"].ToString()!);
            var amount = decimal.Parse(row["Amount"].ToString()!);
			var payee = row["Payee"];
            
            // Act: Create transaction record directly
            var transactionRecord = new TransactionRecord(
                account.Id,
				payee,
                amount,
                date
            );
            
            transactionRecords.Add(transactionRecord);
        }
        
        // Store the transaction records
        context["TransactionRecords"] = transactionRecords;
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

    [When(@"I create the transactions")]
    public void WhenICreateTheTransactions()
    {
        // Actual: Get records from context and convert to objects
        var user = context.Get<User>("User");
        var transactionRecords = context.Get<List<TransactionRecord>>("TransactionRecords");
        var account = user.Accounts.First();
        
        // Act: Convert records to objects
        var transactions = new List<Transaction>();
        int nextTransactionId = 1;
        
        foreach (var record in transactionRecords)
        {
            var transaction = new Transaction(record)
            {
                Id = nextTransactionId++,
                Account = account,
                Payee = "" 
            };
            transactions.Add(transaction);
        }
        
        // Store: Store converted objects
        context["Transactions"] = transactions;
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
    [Then(@"I should have the following transaction entries")]
    public void ThenIShouldHaveTheFollowingTransactionEntries(DataTable dataTable)
    {
        // Inputs (expected)
        var expectedTransactions = dataTable.Rows.Select(row => new
        {
            Date = DateTime.Parse(row["TransactionDate"].ToString()!),
            Amount = decimal.Parse(row["Amount"].ToString()!)
        }).ToList();

        // Actual: Compare against converted objects
        var actualTransactions = context.Get<List<Transaction>>("Transactions");
        // Assert
        actualTransactions.Count.ShouldBe(expectedTransactions.Count);
        
        foreach (var expected in expectedTransactions)
        {
            var actual = actualTransactions.FirstOrDefault(t => 
                t.TransactionDate.Date == expected.Date.Date && t.Amount == expected.Amount);
            
            actual.ShouldNotBeNull($"Transaction with date {expected.Date:yyyy-MM-dd} and amount {expected.Amount} should exist");
            actual!.TransactionDate.Date.ShouldBe(expected.Date.Date);
            actual.Amount.ShouldBe(expected.Amount);
        }
    }

    [Given(@"the created transactions")]
    public void GivenTheCreatedTransactions(DataTable dataTable)
    {
        // Inputs: parse transaction data and create transactions directly
        var user = context.Get<User>("User");
        var account = user.Accounts.First(); // Use first available account
        
        var existingTransactions = context.ContainsKey("Transactions") 
            ? context.Get<List<Transaction>>("Transactions") 
            : new List<Transaction>();
        int nextTransactionId = existingTransactions.Any() ? existingTransactions.Max(t => t.Id) + 1 : 1;
        
        foreach (var row in dataTable.Rows)
        {
            var date = DateTime.Parse(row["Date"].ToString()!);
            var payee = row["Payee"].ToString()!;
            var amount = decimal.Parse(row["Amount"].ToString()!);
            
            // Act: Create transaction record directly
            var record = new TransactionRecord(
                account.Id,
                payee,
                amount,
                date
            );
            
            // Convert to transaction object immediately
            var transaction = new Transaction(record)
            {
                Id = nextTransactionId++
            };
            
            existingTransactions.Add(transaction);
        }
        
        // Store: Store transactions for later use
        context["Transactions"] = existingTransactions;
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
