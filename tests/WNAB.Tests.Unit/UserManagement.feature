Feature: User Management

  In order to establish unique identities
  As a user of the WNAB system
  I want to create users that are active upon creation

  # LLM-Dev: Step definitions pending. Mirrors TransactionEntry style.

  Background:
    Given the system has no existing users

  Scenario: Create a new user
    When I create the following user
      | FirstName | LastName | Email                  |
      | Alice     | Smith    | alice.smith@example.io |
    Then I should have the following user in the system
      | FirstName | LastName | Email                  | IsActive |
      | Alice     | Smith    | alice.smith@example.io | true     |
