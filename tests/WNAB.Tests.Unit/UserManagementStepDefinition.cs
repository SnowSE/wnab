using System;
using Reqnroll;

namespace WNAB.Tests.Unit;

[Binding]
public class StepDefinitions
{
	private readonly ScenarioContext _scenarioContext;

	public StepDefinitions(ScenarioContext scenarioContext)
	{
		_scenarioContext = scenarioContext;
	}

	[Given(@"the system has no existing users")]
	public void Giventhesystemhasnoexistingusers()
	{
		_scenarioContext.Pending();
	}

	[When(@"I create the following user")]
	public void WhenIcreatethefollowinguser()
	{
		_scenarioContext.Pending();
	}

	[Then(@"I should have the following user in the system")]
	public void ThenIshouldhavethefollowinguserinthesystem()
	{
		_scenarioContext.Pending();
	}

}
