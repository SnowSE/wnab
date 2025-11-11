Feature: Ready To Assign Calculation with Snapshots

  In order to efficiently calculate Ready to Assign values across months
  As a user of the WNAB system
  I want to use budget snapshots to track RTA and category balances over time

  Scenario: Create first snapshot from account creation
    Given the account was created on October 15, 2025
    And the following income exists for October 2025
      | Amount | Description        |
      | 500.00 | Initial income     |
    And the following category allocations exist for October 2025
      | CategoryId | BudgetedAmount |
      | 1          | 200.00         |
      | 2          | 150.00         |
    And the following spending exists for October 2025
      | CategoryId | Amount | Description    |
      | 1          | 150.00 | Groceries      |
      | 2          | 100.00 | Gas            |
    When I rebuild snapshots to October 2025
    Then the snapshot for October 2025 should have RTA of 150.00
    And the snapshot should have category 1 with assigned 200.00, activity 150.00, and available 50.00
    And the snapshot should have category 2 with assigned 150.00, activity 100.00, and available 50.00

  Scenario: Build snapshot for next month with previous snapshot
    Given I have a previous snapshot for October 2025 with RTA of 150.00
    And the previous snapshot has category 1 with assigned 200.00, activity 150.00, and available 50.00
    And the previous snapshot has category 2 with assigned 150.00, activity 100.00, and available 50.00
    And the following income exists for November 2025
      | Amount | Description        |
      | 500.00 | Monthly income     |
    And the following category allocations exist for November 2025
      | CategoryId | BudgetedAmount |
      | 1          | 100.00         |
      | 2          | 200.00         |
    And the following spending exists for November 2025
      | CategoryId | Amount | Description    |
      | 1          | 80.00  | Groceries      |
      | 2          | 150.00 | Gas            |
    When I build snapshot from October to November 2025
    Then the snapshot for November 2025 should have RTA of 250.00

  Scenario: Build snapshot with overspending from previous month
    Given I have a previous snapshot for October 2025 with RTA of 100.00
    And the previous snapshot has category 1 with assigned 200.00, activity 250.00, and available -50.00
    And the following income exists for November 2025
      | Amount | Description        |
      | 300.00 | Monthly income     |
    And the following category allocations exist for November 2025
      | CategoryId | BudgetedAmount |
      | 1          | 100.00         |
    And the following spending exists for November 2025
      | CategoryId | Amount | Description    |
      | 1          | 50.00  | Groceries      |
    When I build snapshot from October to November 2025
    Then the snapshot for November 2025 should have RTA of 100.00

  Scenario: Recursively rebuild multiple months of snapshots
    Given the account was created on October 1, 2025
    And the following income exists for October 2025
      | Amount | Description        |
      | 500.00 | Initial income     |
    And the following category allocations exist for October 2025
      | CategoryId | BudgetedAmount |
      | 1          | 300.00         |
    And the following income exists for November 2025
      | Amount | Description        |
      | 500.00 | Monthly income     |
    And the following category allocations exist for November 2025
      | CategoryId | BudgetedAmount |
      | 150.00     | 150.00         |
    And the following income exists for December 2025
      | Amount | Description        |
      | 500.00 | Monthly income     |
    And the following category allocations exist for December 2025
      | CategoryId | BudgetedAmount |
      | 1          | 200.00         |
    When I rebuild snapshots to December 2025
    Then the snapshot for December 2025 should have RTA of 350.00

  Scenario: Calculate RTA with snapshot provided
    Given I have a previous snapshot for October 2025 with RTA of 200.00
    And the previous snapshot has category 1 with assigned 200.00, activity 200.00, and available 0.00
    And the following income exists for November 2025
      | Amount | Description        |
      | 500.00 | Monthly income     |
    And the following category allocations exist for November 2025
      | CategoryId | BudgetedAmount |
      | 1          | 300.00         |
    When I calculate RTA for November 2025 with the snapshot
    Then the RTA should be 400.00

  Scenario: Calculate RTA without snapshot from account creation
    Given the account was created on October 1, 2025
    And the following income exists for October 2025
      | Amount | Description        |
      | 1000.00| Initial income     |
    And the following category allocations exist for October 2025
      | CategoryId | BudgetedAmount |
      | 1          | 300.00         |
      | 2          | 200.00         |
    And the following income exists for November 2025
      | Amount | Description        |
      | 500.00 | Monthly income     |
    And the following category allocations exist for November 2025
      | CategoryId | BudgetedAmount |
      | 1          | 100.00         |
    When I calculate RTA for November 2025 without a snapshot
    Then the RTA should be 900.00

  Scenario: Calculate RTA with overspending in previous snapshot
    Given I have a previous snapshot for October 2025 with RTA of 150.00
    And the previous snapshot has category 1 with assigned 200.00, activity 250.00, and available -50.00
    And the previous snapshot has category 2 with assigned 150.00, activity 200.00, and available -50.00
    And the following income exists for November 2025
      | Amount | Description        |
      | 500.00 | Monthly income     |
    And the following category allocations exist for November 2025
      | CategoryId | BudgetedAmount |
      | 1          | 100.00         |
      | 2          | 100.00         |
    When I calculate RTA for November 2025 with the snapshot
    Then the RTA should be 250.00
