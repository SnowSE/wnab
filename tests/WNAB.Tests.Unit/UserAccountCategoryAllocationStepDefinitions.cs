using System;
using Reqnroll;

namespace WNAB.Tests.Unit;

[Binding]
public class UserAccountCatStepDefinitions
{
	private readonly ScenarioContext _scenarioContext;

	public UserAccountCatStepDefinitions(ScenarioContext scenarioContext)
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


	[Given(@"I create the following account for user ""(.*)""")]
	public void GivenIcreatethefollowingaccountforuser(string args1)
	{
		_scenarioContext.Pending();
	}

	[Then(@"the user ""(.*)"" should have the following accounts")]
	public void Thentheusershouldhavethefollowingaccounts(string args1)
	{
		_scenarioContext.Pending();
	}


	[Given(@"I create the following category for user ""(.*)""")]
	public void GivenIcreatethefollowingcategoryforuser(string args1)
	{
		_scenarioContext.Pending();
	}

	[Given(@"I allocate the following amounts")]
	public void GivenIallocatethefollowingamounts()
	{
		_scenarioContext.Pending();
	}

	[Then(@"I should have the following category allocations for user ""(.*)""")]
	public void ThenIshouldhavethefollowingcategoryallocationsforuser(string args1)
	{
		_scenarioContext.Pending();
	}







	[Given(@"I create the following accounts for user ""(.*)""")]
	public void GivenIcreatethefollowingaccountsforuser(string args1)
	{
		_scenarioContext.Pending();
	}

}