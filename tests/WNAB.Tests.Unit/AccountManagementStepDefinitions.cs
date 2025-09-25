using System;
using Reqnroll;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{

	[Given(@"the following account for user ""(.*)""")]
	public void Giventhefollowingaccountforuser(DataTable dataTable)
	{
		context.Pending();
	}

	[When(@"I create the user and related accounts")]
	public void WhenIcreatetheuserandrelatedaccounts()
	{
		context.Pending();
	}

	[Then(@"the user ""(.*)"" should have the following accounts")]
	public void Thentheusershouldhavethefollowingaccounts(DataTable dataTable)
	{
		context.Pending();
	}

}
