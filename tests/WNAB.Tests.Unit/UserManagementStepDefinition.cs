using System;
using Reqnroll;
using WNAB.Logic.Data;
using Shouldly;

namespace WNAB.Tests.Unit;


public partial class StepDefinitions
{

	[Given(@"the system has no existing users")]
	public void Giventhesystemhasnoexistingusers(DataTable _)
	{
		// LLM-Dev: Initialize empty user list for scenario isolation
		context["Users"] = new List<User>();
	}

	[Given(@"the following user")]
	public void Giventhefollowinguser(DataTable dataTable)
	{
		// LLM-Dev: Parse single-row data table into a transient User object (no persistence layer yet)
		var row = dataTable.Rows.Single();
		var user = new User
		{
			FirstName = row["FirstName"],
			LastName = row["LastName"],
			Email = row["Email"],
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		context["User"] = user; // store the single pending user
	}

	[When(@"I create the user")]
	public void WhenICreateTheUser()
	{
		// LLM-Dev: Simulate persistence by moving the prepared user into Users list
		var user = context.Get<User>("User");
		var users = context.ContainsKey("Users")
			? context.Get<List<User>>("Users")
			: new List<User>();
		// Assign a simple incremental Id
		user.Id = users.Count + 1;
		users.Add(user);
		context["Users"] = users;
	}

	[Then(@"I should have the following user in the system")]
	public void ThenIShouldHaveTheFollowingUserInTheSystem(DataTable dataTable)
	{
		var expectedRow = dataTable.Rows.Single();
		var users = context.Get<List<User>>("Users");
		var email = expectedRow["Email"];
		var actual = users.SingleOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
		actual.ShouldNotBeNull(); // LLM-Dev: Existence check
		actual!.FirstName.ShouldBe(expectedRow["FirstName"]);
		actual.LastName.ShouldBe(expectedRow["LastName"]);
		// If IsActive column provided, assert; else skip (defensive for future scenarios)
		if (dataTable.Header.Contains("IsActive"))
		{
			bool expectedActive = bool.Parse(expectedRow["IsActive"].ToString());
			actual.IsActive.ShouldBe(expectedActive);
		}
	}

}
