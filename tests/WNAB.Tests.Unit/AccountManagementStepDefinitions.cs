using System;
using System.Linq;
using Reqnroll;
using WNAB.Logic;
using WNAB.Logic.Data;
using Shouldly;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
	// LLM-Dev v7.1: Updated to follow prescribed pattern: Given stores records, When converts to objects, Then compares objects
	[Given(@"the following account for user ""(.*)""")]
	public void Giventhefollowingaccountforuser(string email, DataTable dataTable)
	{
		// Inputs: parse account data and create record
		var row = dataTable.Rows.Single();
		var accountName = row["AccountName"];
		var accountType = row["AccountType"]; 
		var openingBalance = decimal.Parse(row["OpeningBalance"]);
		
		// Get user info from context
		var user = context.Get<User>("User");
		
		// Act: Create account record using service
		var accountRecord = AccountManagementService.CreateAccountRecord(accountName, user.Id);
		
		// Store: Store record with additional info needed for conversion
		var accountInfo = new Dictionary<string, object>
		{
			["Record"] = accountRecord,
			["AccountType"] = accountType,
			["OpeningBalance"] = openingBalance,
			["UserEmail"] = email.ToLower()
		};
		
		var recordsKey = $"AccountRecords:{email.ToLower()}";
		var accountRecords = context.ContainsKey(recordsKey)
			? context.Get<List<Dictionary<string, object>>>(recordsKey)
			: new List<Dictionary<string, object>>();
		accountRecords.Add(accountInfo);
		
		context[recordsKey] = accountRecords;
	}

	// Alias for readability in features
	[Given(@"I create the accounts")]
	[When(@"I create the accounts")]
	public void WhenICreateTheAccounts()
	{
		// Actual: get account records and current user
		if (!context.ContainsKey("User"))
		{
			WhenICreateTheUser();
		}
		var user = context.Get<User>("User");
		var emailKey = user.Email.ToLower();
		var recordsKey = $"AccountRecords:{emailKey}";
		var accountRecords = context.ContainsKey(recordsKey)
			? context.Get<List<Dictionary<string, object>>>(recordsKey)
			: new List<Dictionary<string, object>>();

		// Act: convert records to objects
		var accounts = context.ContainsKey("Accounts") ? context.Get<List<Account>>("Accounts") : new List<Account>();
		int nextAccountId = accounts.Any() ? accounts.Max(a => a.Id) + 1 : 1;

		foreach (var accountInfo in accountRecords)
		{
			var record = (AccountRecord)accountInfo["Record"];
			var accountType = (string)accountInfo["AccountType"];
			var openingBalance = (decimal)accountInfo["OpeningBalance"];
			
			// Convert record to object
			var account = new Account(record)
			{
				Id = nextAccountId++,
				AccountType = accountType,
				CachedBalance = openingBalance,
				CachedBalanceDate = DateTime.UtcNow,
				User = user
			};
			accounts.Add(account);
		}
		
		// Store: Store converted objects
		context["Accounts"] = accounts;
		user.Accounts = accounts;
	}

	[Then(@"the user ""(.*)"" should have the following accounts")]
	public void Thentheusershouldhavethefollowingaccounts(string email, DataTable dataTable)
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
