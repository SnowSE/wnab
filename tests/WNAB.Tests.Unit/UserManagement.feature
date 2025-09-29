Feature: User Management

  In order to establish unique identities
  As a user of the WNAB system
  I want to create users that are active upon creation

  # LLM-Dev: Step definitions pending. Mirrors TransactionEntry style.

  # LLM-Dev: Removed Background that cleared all users to match TransactionEntry.feature style.
  # LLM-Dev: New step phrases: "Given the following user" and action "When I create the user".

  Scenario: Create a new user
    Given the following user
      | FirstName | LastName | Email                  |
      | Alice     | Smith    | alice.smith@example.io |
    When I create the user
    Then I should have the following user in the system
      | FirstName | LastName | Email                  | IsActive |
      | Alice     | Smith    | alice.smith@example.io | true     |

  Scenario: Create a user directly via Given
    Given the created user
      | FirstName | LastName | Email                 |
      | Henry     | Jones    | henry.jones@example.io |
    Then I should have the following user in the system
      | FirstName | LastName | Email                  | IsActive |
      | Henry     | Jones    | henry.jones@example.io | true     |
