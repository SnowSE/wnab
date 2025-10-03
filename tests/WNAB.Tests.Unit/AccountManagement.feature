Feature: Account Management

  In order to track where my money is stored
  As a user of the WNAB system
  I want to create financial accounts for a user

  # LLM-Dev: Step definitions pending. Will rely on user creation + account creation steps.

  # LLM-Dev: Removed Background that cleared all users. Each scenario should rely on test isolation just like TransactionEntry.feature.
  # LLM-Dev:v2 New step phrases introduced: "Given the created user", "the following account(s) for user", and "When I create the user and related accounts".

  Scenario: Create a user and a checking account
    Given the created user
      | Id | FirstName | LastName | Email                  |
      | 1  | Bob       | Johnson  | bob.j@example.io       |
    And the following account for user "bob.j@example.io"
      | AccountName      | AccountType | OpeningBalance |
      | EverydayChecking | bank        | 1500.00        |
    And I create the accounts
    Then the user "bob.j@example.io" should have the following accounts
      | AccountName      | AccountType | CachedBalance |
      | EverydayChecking | bank        | 1500.00       |

