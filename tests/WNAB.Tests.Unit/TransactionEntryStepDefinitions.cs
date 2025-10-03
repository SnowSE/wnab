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
            
            // Act: Create transaction record using service
            var transactionRecord = TransactionManagementService.CreateTransactionRecord(
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
                Payee = "" // LLM-Dev: Set required property for basic transaction entries
            };
            transactions.Add(transaction);
        }
        
        // Store: Store converted objects
        context["Transactions"] = transactions;
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
            
            // Act: Create transaction record using service
            var record = TransactionManagementService.CreateTransactionRecord(
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
}
