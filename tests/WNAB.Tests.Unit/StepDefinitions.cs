
using WNAB.Logic;
using WNAB.Logic.Interfaces;

namespace WNAB.Tests.Unit;

[Binding]
public partial class StepDefinitions {

        private readonly ScenarioContext context;
        private readonly ITransactionEntryService transactionEntryService;

        public StepDefinitions(ScenarioContext context)
        {
            this.context = context;
            this.transactionEntryService = new TransactionEntryService();
        }

}