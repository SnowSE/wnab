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
	And the following transaction
		| Date      | Payee   | Category  | Memo     | Amount |
		| 9/10/2025 | Walmart | Groceries | Buy food | 150.00 |
	When I enter the transaction
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
	And the following transaction
		| Date      | Payee   | Memo        | Amount |
		| 9/10/2025 | Walmart | Walmart run | 150.00 |
	When I enter the transaction splits
		| Category      | Amount |
		| Groceries     | 100.00 |
		| Personal Care | 50.00  |
	Then I should have the following transaction splits
		| Category      | Amount |
		| Groceries     | 100.00 |
		| Personal Care | 50.00  |


	