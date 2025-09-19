Feature: TransactionEntry

Shows how a user can enter a transaction in the application.

@tag1
Scenario: Buy groceries
	Given the following transaction
	| Date      | Payee   | Category  | Memo     | Amount |
	| 9/10/2025 | Walmart | Groceries | Buy food | 150.00 |
	When I enter the transaction
	Then I should have the following transaction entry
	| TransactionDate | Amount | Description |
	| 9/10/2025       | 150.00 | Buy food    |
	And I should have the following transaction splits
	| Category  | Amount |
	| Groceries | 150.00 |

