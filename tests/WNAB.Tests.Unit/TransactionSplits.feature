Feature: Transaction Splits

  In order to track spending across multiple categories
  As a user of the WNAB system
  I want to split transactions into multiple categories

  Scenario: Split transaction into single category
    Given the created user
      | Id | FirstName | LastName | Email                |
      | 1  | John      | Doe      | john.doe@example.com |
    And the created accounts
      | AccountName |
      | Checking    |
    And the created categories
      | CategoryName |
      | Groceries    |
    And the created transactions
      | Date      | Payee        | Amount |
      | 9/10/2025 | Grocery Store| 150.00 |
    And the following transaction splits
      | Category  | Amount |
      | Groceries | 150.00 |
    When I create the transaction splits
    Then I should have the following transaction splits
      | Category  | Amount |
      | Groceries | 150.00 |

  Scenario: Split transaction into multiple categories
    Given the created user
      | Id | FirstName | LastName | Email                  |
      | 2  | Jane      | Smith    | jane.smith@example.com |
    And the created accounts
      | AccountName |
      | Checking    |
    And the created categories
      | CategoryName  |
      | Groceries     |
      | Personal Care |
    And the created transactions
      | Date      | Payee      | Amount |
      | 9/10/2025 | Target     | 150.00 |
    And the following transaction splits
      | Category      | Amount |
      | Groceries     | 100.00 |
      | Personal Care | 50.00  |
    When I create the transaction splits
    Then I should have the following transaction splits
      | Category      | Amount |
      | Groceries     | 100.00 |
      | Personal Care | 50.00  |

  Scenario: Split large transaction across many categories
    Given the created user
      | Id | FirstName | LastName | Email                 |
      | 3  | Bob       | Johnson  | bob.j@example.io      |
    And the created accounts
      | AccountName |
      | Checking    |
    And the created categories
      | CategoryName  |
      | Groceries     |
      | Dining        |
      | Gas           |
      | Entertainment |
    And the created transactions
      | Date      | Payee    | Amount |
      | 9/15/2025 | Costco   | 500.00 |
    And the following transaction splits
      | Category      | Amount |
      | Groceries     | 200.00 |
      | Dining        | 150.00 |
      | Gas           | 100.00 |
      | Entertainment | 50.00  |
    When I create the transaction splits
    Then I should have the following transaction splits
      | Category      | Amount |
      | Groceries     | 200.00 |
      | Dining        | 150.00 |
      | Gas           | 100.00 |
      | Entertainment | 50.00  |