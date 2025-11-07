using System;
using System.Linq;
using Reqnroll;
using WNAB.Data;
using WNAB.SharedDTOs;

using Shouldly;

namespace WNAB.Tests.Unit;


public partial class StepDefinitions
{

	// prescribed pattern: (Given) creates and stores records, (When) uses services to create objects, (Then) compares objects
	// Rule: Use the services where possible
	[Given(@"the created user")]
	public void Giventhecreateduser(DataTable dataTable)
	{
		// Inputs (expected)
		if (dataTable == null) throw new ArgumentNullException(nameof(dataTable));
		var row = dataTable.Rows.Single();
		var firstname = row["FirstName"];
		var lastname = row["LastName"];
		var email = row["Email"];
		var userId = dataTable.Header.Contains("Id") ? int.Parse(row["Id"]) : 1;  // Default to 1 if not provided
		
		// Act - create user with ID from feature data (or default)
		User user = new() { 
			Id = userId,
			FirstName = firstname, 
			LastName = lastname, 
			Email = email,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		// Store
		context["User"] = user;
	}

	[Given(@"the system has no existing users")]
	public void Giventhesystemhasnoexistingusers(DataTable _)
	{
		// Store
		context["Users"] = new List<User>();
	}

	[Given(@"the following user")]
	public void Giventhefollowinguser(DataTable dataTable)
	{
		// Inputs (expected)
		var row = dataTable.Rows.Single();
		var firstName = row["FirstName"];
		var lastName = row["LastName"];
		var email = row["Email"];
		// Store the user data for the When step
		context["UserData"] = new { FirstName = firstName, LastName = lastName, Email = email };
	}

	[When(@"I create the user")]
	public void WhenICreateTheUser()
	{
		// Actual
		var userData = context.Get<dynamic>("UserData");
		var users = context.ContainsKey("Users") ? context.Get<List<User>>("Users") : new List<User>();
		// Act
		var user = new User
		{
			Id = users.Count + 1,
			FirstName = userData.FirstName,
			LastName = userData.LastName,
			Email = userData.Email,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		// Store
		users.Add(user);
		context["Users"] = users;
		context["User"] = user;
	}

	[Then(@"I should have the following user in the system")]
	public void ThenIShouldHaveTheFollowingUserInTheSystem(DataTable dataTable)
	{
		// Inputs (expected)
		var expectedRow = dataTable.Rows.Single();
		// Actual
		var actual = context.Get<User>("User");
		// Assert
		actual.FirstName.ShouldBe(expectedRow["FirstName"]);
		actual.LastName.ShouldBe(expectedRow["LastName"]);
		if (dataTable.Header.Contains("Email"))
		{
			actual.Email.ShouldBe(expectedRow["Email"]);
		}
		if (dataTable.Header.Contains("IsActive"))
		{
			bool expectedActive = bool.Parse(expectedRow["IsActive"].ToString());
			actual.IsActive.ShouldBe(expectedActive);
		}
	}

}
