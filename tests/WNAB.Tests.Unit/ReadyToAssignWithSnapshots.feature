Feature: Ready To Assign Calculation with Snapshots

  In order to efficiently calculate Ready to Assign values across months
  As a user of the WNAB system
  I want to use budget snapshots to track RTA and category balances over time

  Scenario: Create first snapshot from account creation
    Given the account was created on October 15, 2025
    And the following income transactions exist
      | Date       | Amount | Description    |
      | 2025-10-16 | 500.00 | Initial income |
    And the following category allocations exist
      | Date       | CategoryId | BudgetedAmount |
      | 2025-10-01 | 1          | 200.00         |
      | 2025-10-01 | 2          | 150.00         |
    And the following spending exists
      | Date       | CategoryId | Amount | Description |
      | 2025-10-20 | 1          | 150.00 | Groceries   |
      | 2025-10-25 | 2          | 100.00 | Gas         |
    When I rebuild snapshots to October 2025
    Then the snapshot for October 2025 should have RTA of 150.00
    And the snapshot should have the following categories
      | CategoryId | Assigned | Activity | Available |
      | 1          | 200.00   | 150.00   | 50.00     |
      | 2          | 150.00   | 100.00   | 50.00     |

  Scenario: Build snapshot for next month with previous snapshot
    Given I have a previous snapshot with the following details
      | Month | Year | RTA    |
      | 10    | 2025 | 150.00 |
    And the previous snapshot has the following categories
      | CategoryId | Assigned | Activity | Available |
      | 1          | 200.00   | 150.00   | 50.00     |
      | 2          | 150.00   | 100.00   | 50.00     |
    And the following income transactions exist
      | Date       | Amount | Description    |
      | 2025-11-05 | 500.00 | Monthly income |
    And the following category allocations exist
      | Date       | CategoryId | BudgetedAmount |
      | 2025-11-01 | 1          | 100.00         |
      | 2025-11-01 | 2          | 200.00         |
    And the following spending exists
      | Date       | CategoryId | Amount | Description |
      | 2025-11-10 | 1          | 80.00  | Groceries   |
      | 2025-11-15 | 2          | 150.00 | Gas         |
    When I build snapshot from October to November 2025
    Then the snapshot for November 2025 should have RTA of 250.00

  Scenario: Build snapshot with overspending from previous month
    Given I have a previous snapshot with the following details
      | Month | Year | RTA    |
      | 10    | 2025 | 100.00 |
    And the previous snapshot has the following categories
      | CategoryId | Assigned | Activity | Available |
      | 1          | 200.00   | 250.00   | -50.00    |
    And the following income transactions exist
      | Date       | Amount | Description    |
      | 2025-11-01 | 300.00 | Monthly income |
    And the following category allocations exist
      | Date       | CategoryId | BudgetedAmount |
      | 2025-11-01 | 1          | 100.00         |
    And the following spending exists
      | Date       | CategoryId | Amount | Description |
      | 2025-11-12 | 1          | 50.00  | Groceries   |
    When I build snapshot from October to November 2025
    Then the snapshot for November 2025 should have RTA of 100.00

  Scenario: Recursively rebuild multiple months of snapshots
    Given the account was created on October 1, 2025
    And the following income transactions exist
      | Date       | Amount | Description    |
      | 2025-10-05 | 500.00 | Initial income |
      | 2025-11-05 | 500.00 | Monthly income |
      | 2025-12-05 | 500.00 | Monthly income |
    And the following category allocations exist
      | Date       | CategoryId | BudgetedAmount |
      | 2025-10-01 | 1          | 300.00         |
      | 2025-11-01 | 1          | 150.00         |
      | 2025-12-01 | 1          | 200.00         |
    When I rebuild snapshots to December 2025
    Then the snapshot for December 2025 should have RTA of 350.00

  Scenario: Calculate RTA with snapshot provided
    Given I have a previous snapshot with the following details
      | Month | Year | RTA    |
      | 10    | 2025 | 200.00 |
    And the previous snapshot has the following categories
      | CategoryId | Assigned | Activity | Available |
      | 1          | 200.00   | 200.00   | 0.00      |
    And the following income transactions exist
      | Date       | Amount | Description    |
      | 2025-11-01 | 500.00 | Monthly income |
    And the following category allocations exist
      | Date       | CategoryId | BudgetedAmount |
      | 2025-11-01 | 1          | 300.00         |
    When I calculate RTA for November 2025 with the snapshot
    Then the RTA should be 400.00

  Scenario: Calculate RTA without snapshot from account creation
    Given the account was created on October 1, 2025
    And the following income transactions exist
      | Date       | Amount  | Description    |
      | 2025-10-05 | 1000.00 | Initial income |
      | 2025-11-05 | 500.00  | Monthly income |
    And the following category allocations exist
      | Date       | CategoryId | BudgetedAmount |
      | 2025-10-01 | 1          | 300.00         |
      | 2025-10-01 | 2          | 200.00         |
      | 2025-11-01 | 1          | 100.00         |
    When I calculate RTA for November 2025 without a snapshot
    Then the RTA should be 900.00

  Scenario: Calculate RTA with overspending in previous snapshot
    Given I have a previous snapshot with the following details
      | Month | Year | RTA    |
      | 10    | 2025 | 150.00 |
    And the previous snapshot has the following categories
      | CategoryId | Assigned | Activity | Available |
      | 1          | 200.00   | 250.00   | -50.00    |
      | 2          | 150.00   | 200.00   | -50.00    |
    And the following income transactions exist
      | Date       | Amount | Description    |
      | 2025-11-01 | 500.00 | Monthly income |
    And the following category allocations exist
      | Date       | CategoryId | BudgetedAmount |
      | 2025-11-01 | 1          | 100.00         |
      | 2025-11-01 | 2          | 100.00         |
    When I calculate RTA for November 2025 with the snapshot
    Then the RTA should be 250.00
