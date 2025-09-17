# WNAB Database Entity Relationship Diagram

```mermaid
erDiagram
    User {
        int UserId PK
        string Email UK
        string PasswordHash
        string FirstName
        string LastName
        datetime CreatedAt
        datetime UpdatedAt
        bool IsActive
    }
    
    Category {
        int CategoryId PK
        int UserId FK
        string Name
        string Description
        string Color
        decimal BudgetAmount
        bool IsIncome
        datetime CreatedAt
        datetime UpdatedAt
        bool IsActive
    }
    
    Account {
        int AccountId PK
        int UserId FK
        string AccountName
        string AccountType
        decimal Balance
        string PlaidAccountId
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    Transaction {
        int TransactionId PK
        int AccountId FK
        int CategoryId FK
        string Description
        decimal Amount
        datetime TransactionDate
        string PlaidTransactionId
        bool IsReconciled
        string Notes
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    Budget {
        int BudgetId PK
        int UserId FK
        int CategoryId FK
        decimal BudgetedAmount
        int Month
        int Year
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    %% Relationships
    User ||--o{ Category : "creates"
    User ||--o{ Account : "owns"
    User ||--o{ Budget : "sets"
    
    Account ||--o{ Transaction : "contains"
    Category ||--o{ Transaction : "categorizes"
    Category ||--o{ Budget : "budgets"
```

## Entity Descriptions

### Core Entities

**User**
- Primary entity representing a WNAB user
- Contains authentication and profile information

**Category**
- Budget categories (e.g., "Groceries", "Rent", "Salary")
- Can be income or expense categories
- User-specific with budget amounts

**Account**
- Financial accounts (checking, savings, credit cards)
- Linked to Plaid for automatic transaction import
- Tracks current balance

**Transaction**
- Individual financial transactions
- Links accounts to categories
- Can be imported from Plaid or manually entered

### Supporting Entities

**Budget**
- Monthly budget allocations per category
- Allows tracking spending vs. budgeted amounts

## Key Design Features

1. **User Isolation**: All entities are scoped to users for multi-tenant support
2. **Plaid Integration**: Fields for storing Plaid IDs for automatic syncing
3. **Soft Deletes**: IsActive flags instead of hard deletes
4. **Audit Trail**: CreatedAt/UpdatedAt timestamps on all entities
5. **Simple Design**: Clean MVP structure without complex splitting features
6. **Budget Tracking**: Monthly budget allocations with comparison to actual spending