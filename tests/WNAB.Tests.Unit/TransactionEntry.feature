Feature: TransactionEntry

Shows how a user can enter a transaction in the application.

@tag1
Scenario: Buy groceries
	Given the created user
		| FirstName | LastName | Email                |
		| John      | Doe      | john.doe@example.com |
	And the following account for user "john.doe@example.com"
		| AccountName     | AccountType | OpeningBalance |
		| Checking        | Checking    | 1000.00        |
	And I create the accounts
	And the following category for user "john.doe@example.com"
		| CategoryName |
		| Groceries    |
	And the following budget allocation for September 2025
		| CategoryName | BudgetedAmount |
		| Groceries    | 500.00         |
	And the following transaction
		| Date      | Payee   | Memo     | Amount |
		| 9/10/2025 | Walmart | Buy food | 150.00 |
	When I enter the transaction with split
		| Category  | Amount |
		| Groceries | 150.00 |
	Then I should have the following transaction entry
		| TransactionDate | Amount | Memo     |
		| 9/10/2025       | 150.00 | Buy food |
	And I should have the following transaction splits
		| Category  | Amount |
		| Groceries | 150.00 |

Scenario: Buy groceries and personal care
	Given the created user
		| FirstName | LastName | Email                |
		| Jane      | Smith    | jane.smith@example.com |
	And the following account for user "jane.smith@example.com"
		| AccountName     | AccountType | OpeningBalance |
		| Checking        | Checking    | 1000.00        |
	And I create the accounts
	And the following categories for user "jane.smith@example.com"
		| CategoryName  |
		| Groceries     |
		| Personal Care |
	And the following budget allocation for September 2025
		| CategoryName  | BudgetedAmount |
		| Groceries     | 500.00         |
		| Personal Care | 200.00         |
	And the following transaction
		| Date      | Payee   | Memo        | Amount |
		| 9/10/2025 | Walmart | Walmart run | 150.00 |
	When I enter the transaction with split
		| Category      | Amount |
		| Groceries     | 100.00 |
		| Personal Care | 50.00  |
	Then I should have the following transaction splits
		| Category      | Amount |
		| Groceries     | 100.00 |
		| Personal Care | 50.00  |

Scenario: Cannot create transaction without budget allocation
	Given the created user
		| FirstName | LastName | Email                  |
		| Bob       | Johnson  | bob.johnson@example.com |
	And the following account for user "bob.johnson@example.com"
		| AccountName | AccountType | OpeningBalance |
		| Checking    | Checking    | 1000.00        |
	And I create the accounts
	And the following category for user "bob.johnson@example.com"
		| CategoryName |
		| Groceries    |
	And the following transaction
		| Date      | Payee   | Memo     | Amount |
		| 9/10/2025 | Walmart | Buy food | 150.00 |
	When I attempt to enter the transaction with split
		| Category  | Amount |
		| Groceries | 150.00 |
	Then the transaction creation should fail with message "No budget allocation found for category 'Groceries' in September 2025"


	