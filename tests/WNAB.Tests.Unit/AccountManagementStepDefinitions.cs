using System;
using System.Collections.Generic;
using System.Linq;
using Reqnroll;
using WNAB.Logic.Data;
using WNAB.Logic;
using Shouldly;

namespace WNAB.Tests.Unit;


public partial class StepDefinitions
{

    // prescribed pattern: (Given) creates and stores records, (When) create objects using constructors, (Then) compares objects
	// Rule: Use the services where possible
	
	[Given(@"the following account")]
	public void GivenTheFollowingAccount(DataTable dataTable)
	{
		// Inputs: parse account data from table
		var row = dataTable.Rows[0];
		var accountName = row["AccountName"];
		var accountType = row["AccountType"];
		var openingBalance = decimal.Parse(row["OpeningBalance"]);
		
		// Get user from context
		var user = context.Get<User>("User");
		
		// Act: Create account record using service
		var accountRecord = AccountManagementService.CreateAccountRecord(accountName, user.Id);
		// Note: OpeningBalance and AccountType aren't in the service method, so we'll set them after creation
		
		// Store the account record
		context["AccountRecord"] = accountRecord;
	}

	[Given(@"the following account for user")]
	public void Giventhefollowingaccountforuser(DataTable dataTable)
	{
		// Inputs (expected)
		var row = dataTable.Rows.Single();
		var accountName = row["AccountName"];
		
		// Actual
		var user = context.Get<User>("User");
		
		// Act: Create account record using service
		var accountRecord = AccountManagementService.CreateAccountRecord(accountName, user.Id);
		
		// Store
		context["AccountRecord"] = accountRecord;
	}

	[Given(@"I create the accounts")]
	public void GivenICreateTheAccounts()
	{
		// Actual - get required context
		var user = context.Get<User>("User");
		var accountRecord = context.Get<AccountRecord>("AccountRecord");

		//act
		var account = new Account(accountRecord)
		// the only thing that should ever be set here is an ID!
		{
			Id = 1 // Set test ID
		};
		
		// Initialize user accounts if not already done
		if (user.Accounts == null)
			user.Accounts = new List<Account>();
		
		user.Accounts.Add(account);
		
		// Store: Store the accounts list for context
		context["Accounts"] = user.Accounts.ToList();
	}

	

	[When(@"I create the accounts")]
	public void WhenICreateTheAccounts()
	{
		// Actual - get required context (user and record should already exist from Given steps)
		var user = context.Get<User>("User");
		var record = context.Get<AccountRecord>("AccountRecord");
		var accounts = context.ContainsKey("Accounts") ? context.Get<List<Account>>("Accounts") : new List<Account>();

		// Act
		var account = new Account(record)
		// only ever set the ID here, nothing else
		{
			Id = accounts.Count + 1
		};
		accounts.Add(account);
		
		// Store objects only - update user.Accounts to maintain object relationships needed for Then steps
		user.Accounts = accounts;
		context["Accounts"] = accounts;
	}

	[Then(@"the user should have the following accounts")]
	public void Thentheusershouldhavethefollowingaccounts(DataTable dataTable)
	{
		// Inputs (expected)
		var expectedRows = dataTable.Rows.ToList();
		// Actual
		var user = context.Get<User>("User");
		var accounts = user.Accounts;
		// Assert
		accounts.Count.ShouldBe(expectedRows.Count);
		for (int i = 0; i < expectedRows.Count; i++)
		{
			var row = expectedRows[i];
			var match = accounts.FirstOrDefault(a => a.AccountName == row["AccountName"]);
			match.ShouldNotBeNull();
			match!.AccountType.ShouldBe(row["AccountType"]);
			if (dataTable.Header.Contains("CachedBalance"))
			{
				match.CachedBalance.ShouldBe(decimal.Parse(row["CachedBalance"]));
			}
		}
	}
}
