Feature: Account DB Service
 In order to verify account data persistence rules
 As a developer
 I want to describe AccountDBService behaviors with Gherkin

 Scenario: Create account saves one entry and returns it
 Given an empty in-memory database
 And a user exists with Id1 and email "u@test.com"
 When I create an account named "Checking"
 Then exactly1 account should exist for user1
 And the created account should have name "Checking" and a generated Id

 Scenario: Fails when there are pending changes
 Given an empty in-memory database
 And a user exists with Id1 and email "u@test.com"
 And a pending category change exists
 When I attempt to create an account named "Checking"
 Then the operation should fail with InvalidOperationException

 Scenario: Fails on null user
 Given an empty in-memory database
 When I attempt to create an account with a null user
 Then the operation should fail with ArgumentNullException

 Scenario: Fails on blank name
 Given an empty in-memory database
 And a user exists with Id1 and email "u@test.com"
 When I attempt to create an account with a blank name
 Then the operation should fail with ArgumentException
