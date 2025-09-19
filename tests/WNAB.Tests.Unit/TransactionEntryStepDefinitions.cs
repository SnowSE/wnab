using System;
using Reqnroll;
using Shouldly;
using WNAB.Logic.Data;

namespace WNAB.Tests.Unit
{
    [Binding]
    public class TransactionEntryStepDefinitions
    {
        private readonly ScenarioContext context;

        public TransactionEntryStepDefinitions(ScenarioContext context)
        {
            this.context = context;
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
            var transactionEntryVM = context.Get<TransactionEntryViewModel>("TransactionEntryVM");

            //todo: create that
            var transaction = transactionEntryService.AddTransaction(transactionEntryVM);
            context["Transaction"] = transaction;
        }

        [Then("I should have the following transaction entry")]
        public void ThenIShouldHaveTheFollowingTransactionEntry(DataTable dataTable)
        {
            var expectedTransaction = dataTable.CreateInstance<TransactionEntryViewModel>();
            var actualTransaction = context.Get<TransactionEntryViewModel>("Transaction");            
            actualTransaction.ShouldBeEquivalentTo(expectedTransaction);
        }

        [Then("I should have the following transaction splits")]
        public void ThenIShouldHaveTheFollowingTransactionSplits(DataTable dataTable)
        {
            var actualTransaction = context.Get<TransactionEntryViewModel>("Transaction");
            var expectedSplits = dataTable.CreateSet<TransactionSplit>();
            actualTransaction.Splits.ShouldBeEquivalentTo(expectedSplits);
        }
    }

    //todo: move this into logic somewhere and actually make it a vm
    public class TransactionEntryViewModel
    {
        public DateTime Date { get; set; }
        public string Payee { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Memo { get; set; } = string.Empty;
    }
}
