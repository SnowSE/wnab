using System;
using Reqnroll;

namespace WNAB.Tests.Unit;

[Binding]
public class AccountStepDefinitions
{
	private readonly ScenarioContext _scenarioContext;

	public AccountStepDefinitions(ScenarioContext scenarioContext)
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

	[Given(@"I create the following accounts for user ""(.*)""")]
	public void GivenIcreatethefollowingaccountsforuser(string args1)
	{
		_scenarioContext.Pending();
	}



}