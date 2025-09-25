using System;
using System.Linq;
using Reqnroll;
using Shouldly;
using WNAB.Logic;
using WNAB.Logic.Data;
using WNAB.Logic.Interfaces;
using WNAB.Logic.ViewModels;

namespace WNAB.Tests.Unit
{
    [Binding]
    public class TransactionEntryStepDefinitions
    {
        private readonly ScenarioContext context;
        private readonly ITransactionEntryService transactionEntryService;

        public TransactionEntryStepDefinitions(ScenarioContext context)
        {
            this.context = context;
            this.transactionEntryService = new TransactionEntryService();
        }

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

            var processedTransaction = transactionEntryService.AddTransaction(transactionEntryVM);
            
            context["Transaction"] = processedTransaction;
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
            
            // Convert the table to TransactionSplit objects
            var splits = dataTable.Rows.Select(row => new TransactionSplit
            {
                Amount = decimal.Parse(row["Amount"].ToString()),
                CategoryName = row["Category"].ToString()
            }).ToList();
            
            var updatedTransaction = transactionEntryService.AddTransactionSplits(transactionEntryVM, splits);
            
            context["Transaction"] = updatedTransaction;
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
            }
        }
    }
}
