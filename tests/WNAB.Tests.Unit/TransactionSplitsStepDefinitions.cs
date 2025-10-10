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

    [Given(@"the following transaction splits")]
    public void GivenTheFollowingTransactionSplits(DataTable dataTable)
    {
        // Get user and categories from context
        var user = context.Get<User>("User");
        var categories = user.Categories.ToList();
        var transactions = context.Get<List<Transaction>>("Transactions");
        var transaction = transactions.First(); // Use first available transaction
        
        // Create TransactionSplitRecord objects from the table using the service method
        var splitRecords = new List<TransactionSplitRecord>();
        foreach (var row in dataTable.Rows)
        {
            var categoryName = row["Category"].ToString()!;
            var amount = decimal.Parse(row["Amount"].ToString()!);
            var category = categories.Single(c => c.Name == categoryName);
            
            // Create split record using service method
            var splitRecord = new TransactionSplitRecord(
                category.Id, 
                transaction.Id, 
                amount
            );
            splitRecords.Add(splitRecord);
        }
        
        // Store the split records
        context["TransactionSplitRecords"] = splitRecords;
    }

    [When(@"I create the transaction splits")]
    public void WhenICreateTheTransactionSplits()
    {
        // Actual: Get records from context
        var user = context.Get<User>("User");
        var categories = user.Categories.ToList();
        var transactions = context.Get<List<Transaction>>("Transactions");
        var transaction = transactions.First();
        var splitRecords = context.Get<List<TransactionSplitRecord>>("TransactionSplitRecords");
        
        // Act: Convert records to objects
        var transactionSplits = new List<TransactionSplit>();
        int nextSplitId = 1;
        
        foreach (var splitRecord in splitRecords)
        {
            var category = categories.Single(c => c.Id == splitRecord.CategoryId);
            var split = new TransactionSplit(splitRecord)
            {
                Id = nextSplitId++,
                Category = category,
                Transaction = transaction
            };
            transactionSplits.Add(split);
        }
        
        // Associate splits with transaction
        transaction.TransactionSplits = transactionSplits;
        
        // Store: Store converted objects
        context["TransactionSplits"] = transactionSplits;
    }

    [Then(@"I should have the following transaction splits")]
    public void ThenIShouldHaveTheFollowingTransactionSplits(DataTable dataTable)
    {
        // Inputs (expected)
        var expectedSplits = dataTable.Rows.Select(row => new
        {
            Category = row["Category"].ToString()!,
            Amount = decimal.Parse(row["Amount"].ToString()!)
        }).ToList();

        // Actual - get splits from converted objects
        var actualSplits = context.Get<List<TransactionSplit>>("TransactionSplits");
        var user = context.Get<User>("User");
        var categories = user.Categories.ToList();

        // Assert
        actualSplits.Count.ShouldBe(expectedSplits.Count);
        
        foreach (var expectedSplit in expectedSplits)
        {
            var expectedCategory = categories.Single(c => c.Name == expectedSplit.Category);
            var actualSplit = actualSplits.FirstOrDefault(s => 
                s.CategoryId == expectedCategory.Id && s.Amount == expectedSplit.Amount);
            
            actualSplit.ShouldNotBeNull($"Split for category '{expectedSplit.Category}' with amount {expectedSplit.Amount} should exist");
            actualSplit!.Amount.ShouldBe(expectedSplit.Amount);
            actualSplit.CategoryId.ShouldBe(expectedCategory.Id);
        }
    }
}