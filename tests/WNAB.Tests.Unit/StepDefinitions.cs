
using WNAB.Logic;
// using WNAB.Logic.Interfaces;
using WNAB.Logic.Data;
using Shouldly;

namespace WNAB.Tests.Unit;

[Binding]
public partial class StepDefinitions {

		// a note: This part of the class should contain only major definitions, such as context.
		// All services that are given to the class for testing should be instanced in their specific stepdefinition file.
		// because this class is partial, if there were to be other places one must use those interfaces/services in other files
		// they will be included because at compile time all of this is smashed together.
        private readonly ScenarioContext context;
        public StepDefinitions(ScenarioContext context)
        {
            this.context = context;
        }


}