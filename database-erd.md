# WNAB Database Entity Relationship Diagram

Here's a diagram

```mermaid
erDiagram
    User {
        int Id PK
        string Email UK
        string FirstName
        string LastName
        datetime CreatedAt
        datetime UpdatedAt
        bool IsActive
    }
    
    Category {
        int Id PK
        int UserId FK
        string Name
        string Description
        string Color
        bool IsIncome
        datetime CreatedAt
        datetime UpdatedAt
        bool IsActive
    }
    
    Account {
        int Id PK
        int UserId FK
        string AccountName
        string AccountType
        decimal CachedBalance
        datetime CachedBalanceDate
        string PlaidAccountId
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    Transaction {
        int Id PK
        int AccountId FK
        string Description
        decimal Amount
        datetime TransactionDate
        string PlaidTransactionId
        bool IsReconciled
        string Notes
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    %% Unique constraint: (CategoryId, Year, Month)
    CategoryAllocation {
        int Id PK
        int CategoryId FK
        decimal BudgetedAmount
        int Month
        int Year
        datetime CreatedAt
        datetime UpdatedAt
    }

    TransactionSplit {
        int Id PK
        int TransactionId FK
        int CategoryId FK
        decimal Amount
        string Notes
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    %% Relationships
    User ||--o{ Category : "creates"
    User ||--o{ Account : "owns"
    
    Account ||--o{ Transaction : "contains"
    Transaction ||--o{ TransactionSplit : "is split into"
    Category ||--o{ TransactionSplit : "categorizes"
    Category ||--o{ CategoryAllocation : "budgets"
```

## Entity Descriptions

### Core Entities

**User**
- Primary entity representing a WNAB user
- Contains authentication and profile information

**Category**
- Budget categories (e.g., "Groceries", "Rent", "Salary")

**Account**
- Financial accounts (checking, savings, credit cards)
- Potentially linked to Plaid for automatic transaction import
- Tracks current balance (cached, updated at times)

**Transaction**
- Individual financial transactions
- Can be imported from Plaid or manually entered

**TransactionSplit**
- Links transaction to category
- If the transactions is not split (e.g. the entire transaction amount is for a single category, the system creates a single TransactionSplit behind the scenes for the entire amount).  
- Every transaction has at least 1 TransactionSplit record.

### Supporting Entities

**CategoryAllocation**
- Monthly budget allocations per category
- Allows tracking spending vs. budgeted amounts
- Unique constraint: (CategoryId, Year, Month)

## Key Design Features

1. **User Isolation**: All entities are scoped to users for multi-tenant support
2. **Plaid Integration**: Fields for storing Plaid IDs for automatic syncing
3. **Soft Deletes**: IsActive flags instead of hard deletes for Account and Category
4. **Audit Trail**: CreatedAt/UpdatedAt timestamps on all entities
6. **Budget Tracking**: Monthly budget allocations with comparison to actual spending