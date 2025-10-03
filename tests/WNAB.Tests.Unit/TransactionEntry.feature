Feature: Transaction Entry

  In order to track my spending
  As a user of the WNAB system
  I want to create transaction entries

  @tag1
  Scenario: Create a simple transaction
    Given the created user
      | Id | FirstName | LastName | Email                |
      | 1  | John      | Doe      | john.doe@example.com |
    And the created accounts
      | AccountName |
      | Checking    |
    And the following transactions
      | Date      | Payee        | Amount |
      | 9/10/2025 | Test Store   | 150.00 |
    When I create the transactions
    Then I should have the following transaction entries
      | TransactionDate | Amount |
      | 9/10/2025       | 150.00 |

  Scenario: Create multiple transactions
    Given the created user
      | Id | FirstName | LastName | Email                  |
      | 2  | Jane      | Smith    | jane.smith@example.com |
    And the created accounts
      | AccountName |
      | Checking    |
    And the following transactions
      | Date      | Payee           | Amount |
      | 9/10/2025 | Grocery Store   | 150.00 |
      | 9/11/2025 | Gas Station     | 75.50  |
      | 9/12/2025 | Restaurant      | 200.00 |
    When I create the transactions
    Then I should have the following transaction entries
      | TransactionDate | Amount |
      | 9/10/2025       | 150.00 |
      | 9/11/2025       | 75.50  |
      | 9/12/2025       | 200.00 |

  Scenario: Create transaction with different amounts
    Given the created user
      | Id | FirstName | LastName | Email                 |
      | 3  | Bob       | Johnson  | bob.j@example.io      |
    And the created accounts
      | AccountName |
      | Savings     |
    And the following transactions
      | Date      | Payee       | Amount  |
      | 9/15/2025 | Bank        | 1000.00 |
    When I create the transactions
    Then I should have the following transaction entries
      | TransactionDate | Amount  |
      | 9/15/2025       | 1000.00 |


	