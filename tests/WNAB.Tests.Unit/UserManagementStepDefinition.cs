using System;
using Reqnroll;

namespace WNAB.Tests.Unit;


public partial class StepDefinitions
{

	[Given(@"the system has no existing users")]
	public void Giventhesystemhasnoexistingusers(DataTable dataTable)
	{
		context.Pending();
	}

	[Given(@"the following user")]
	public void Giventhefollowinguser(DataTable dataTable)
	{
		context.Pending();
	}

	[When(@"I create the following user")]
	public void WhenIcreatethefollowinguser()
	{
		context.Pending();
	}

	[Then(@"I should have the following user in the system")]
	public void ThenIshouldhavethefollowinguserinthesystem(DataTable dataTable)
	{
		context.Pending();
	}

}
