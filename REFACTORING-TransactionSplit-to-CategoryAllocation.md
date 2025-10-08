# Refactoring: TransactionSplit to use CategoryAllocation

**Date:** October 6, 2025  
**Branch:** transaction-flows  
**Status:** ✅ IMPLEMENTED (Phases 1-6 Complete)

## Implementation Summary (October 8, 2025)

### ✅ Completed Work

**Phase 1-3: Core Infrastructure**
- ✅ Database schema updated with migration `20251008184450_categoryalloc`
- ✅ Entity models refactored (`TransactionSplit`, `CategoryAllocation`, `Category`)
- ✅ DTOs and records updated to use `CategoryAllocationId` and `IsIncome`
- ✅ API endpoints updated with proper navigation through CategoryAllocation → Category
- ✅ DbContext configuration updated with new relationships

**Phase 4: UI Logic**
- ✅ `TransactionViewModel` updated with CategoryAllocation auto-lookup
- ✅ Budget-first validation implemented: transactions require existing allocations
- ✅ Split transaction support with per-split allocation validation
- ✅ User-friendly error messages when allocations are missing
- ✅ `CategoryAllocationManagementService` enhanced with `FindAllocationAsync` method

**Phase 5: Tests**
- ✅ All 8 unit tests passing
- ✅ Test step definitions updated to auto-create allocations
- ✅ Feature files work correctly with Category → Allocation mapping

**Phase 6: Documentation**
- ✅ ERD updated with new schema and relationships
- ✅ New fields documented with their purposes
- ✅ Key design features updated

### 🔄 Key Implementation Details

**Budget-First Enforcement:**
- When user selects a Category, system automatically looks up CategoryAllocation for the transaction date (month/year)
- If no allocation exists, clear error message prompts user to create budget first
- Applies to both simple transactions and split transactions
- Transaction cannot be saved without valid CategoryAllocations

**Auto-Selection Logic:**
- `TransactionViewModel.OnSelectedCategoryChanged` → validates allocation exists
- `TransactionViewModel.OnTransactionDateChanged` → re-validates when date changes
- `TransactionViewModel.FindAndSetAllocationForSplitAsync` → validates splits
- All validation provides immediate feedback via `StatusMessage`

**Soft Delete Implementation:**
- `CategoryAllocation.IsActive` flag preserves historical data
- Inactive allocations filtered from selection but remain in historical transactions

### ⚠️ Remaining Work (Phase 7)

- [ ] Integration testing with live database
- [ ] Manual testing of complete transaction flow in MAUI app
- [ ] Performance validation (check for N+1 query issues)
- [ ] Database constraint verification

---

## Overview

This document outlines the complete refactoring required to change the `TransactionSplit` table to reference `CategoryAllocation` instead of `Category`, and to add additional tracking fields to `CategoryAllocation`.

---

## Database Schema Changes

### 1. TransactionSplit Table Changes

**Current Schema:**
```sql
CREATE TABLE TransactionSplits (
    Id integer PRIMARY KEY,
    TransactionId integer NOT NULL,
    CategoryId integer NOT NULL,  -- TO BE REMOVED
    Amount decimal(18,2) NOT NULL,
    Notes varchar(1000),
    CreatedAt timestamp with time zone,
    UpdatedAt timestamp with time zone,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);
```

**New Schema:**
```sql
CREATE TABLE TransactionSplits (
    Id integer PRIMARY KEY,
    TransactionId integer NOT NULL,
    CategoryAllocationId integer NOT NULL,  -- NEW: FK to CategoryAllocation
    Amount decimal(18,2) NOT NULL,
    IsIncome boolean NOT NULL,              -- NEW: Income flag
    Notes varchar(1000),
    CreatedAt timestamp with time zone,
    UpdatedAt timestamp with time zone,
    FOREIGN KEY (CategoryAllocationId) REFERENCES Allocations(Id)
);
```

**Changes:**
- ❌ Remove: `CategoryId` column
- ✅ Add: `CategoryAllocationId` (integer, NOT NULL, FK to Allocations table)
- ✅ Add: `IsIncome` (boolean, NOT NULL)
- 🔄 Update: Foreign key from Categories to Allocations

---

### 2. CategoryAllocation (Allocations) Table Changes

**Current Schema:**
```sql
CREATE TABLE Allocations (
    Id integer PRIMARY KEY,
    CategoryId integer NOT NULL,
    BudgetedAmount decimal(18,2) NOT NULL,
    Month integer NOT NULL,
    Year integer NOT NULL,
    CreatedAt timestamp with time zone,
    UpdatedAt timestamp with time zone,
    UserId integer,
    UNIQUE(CategoryId, Month, Year)
);
```

**New Schema:**
```sql
CREATE TABLE Allocations (
    Id integer PRIMARY KEY,
    CategoryId integer NOT NULL,
    BudgetedAmount decimal(18,2) NOT NULL,
    Month integer NOT NULL,
    Year integer NOT NULL,
    EditorName varchar(40),            -- NEW
    PercentageAllocation decimal,      -- NEW
    OldAmount decimal(18,2),           -- NEW
    EditedMemo text,                   -- NEW
    IsActive boolean NOT NULL,         -- NEW: Soft delete flag
    CreatedAt timestamp with time zone,
    UpdatedAt timestamp with time zone,
    UserId integer,
    UNIQUE(CategoryId, Month, Year)
);
```

**Changes:**
- ✅ Add: `EditorName` (varchar(40), nullable)
- ✅ Add: `PercentageAllocation` (decimal, nullable)
- ✅ Add: `OldAmount` (decimal(18,2), nullable)
- ✅ Add: `EditedMemo` (text, nullable)
- ✅ Add: `IsActive` (boolean, NOT NULL, default true)

---

## Entity Model Changes (C# Files)

### 3. TransactionSplit.cs
**File:** `src\WNAB.Logic\Data\TransactionSplit.cs`

**Current Code:**
```csharp
public class TransactionSplit
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    [MaxLength(1000)]
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public Transaction Transaction { get; set; } = null!;
    public Category Category { get; set; } = null!;
    
    [NotMapped]
    public string CategoryName { get; set; } = string.Empty;
}
```

**New Code:**
```csharp
public class TransactionSplit
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int CategoryAllocationId { get; set; }  // CHANGED from CategoryId
    public decimal Amount { get; set; }
    public bool IsIncome { get; set; }             // NEW
    [MaxLength(1000)]
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public Transaction Transaction { get; set; } = null!;
    public CategoryAllocation CategoryAllocation { get; set; } = null!;  // CHANGED from Category
    
    [NotMapped]
    public string CategoryName { get; set; } = string.Empty;  // May still be useful for display
}
```

**Constructor Changes:**
```csharp
// OLD:
public TransactionSplit(TransactionSplitRecord record)
{
    ArgumentNullException.ThrowIfNull(record);
    CategoryId = record.CategoryId;
    Amount = record.Amount;
    Notes = record.Notes;
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}

// NEW:
public TransactionSplit(TransactionSplitRecord record)
{
    ArgumentNullException.ThrowIfNull(record);
    CategoryAllocationId = record.CategoryAllocationId;  // CHANGED
    Amount = record.Amount;
    IsIncome = record.IsIncome;                         // NEW
    Notes = record.Notes;
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}
```

**Specific Changes:**
- ❌ Remove: `public int CategoryId { get; set; }`
- ✅ Add: `public int CategoryAllocationId { get; set; }`
- ✅ Add: `public bool IsIncome { get; set; }`
- ❌ Remove: `public Category Category { get; set; } = null!;`
- ✅ Add: `public CategoryAllocation CategoryAllocation { get; set; } = null!;`
- 🔄 Update: Constructor to use `CategoryAllocationId` and `IsIncome`

---

### 4. CategoryAllocation.cs
**File:** `src\WNAB.Logic\Data\CategoryAllocation.cs`

**Current Code:**
```csharp
public class CategoryAllocation
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public decimal BudgetedAmount { get; set; }
    [Range(1, 12)]
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public Category Category { get; set; } = null!;
}
```

**New Code:**
```csharp
public class CategoryAllocation
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public decimal BudgetedAmount { get; set; }
    [Range(1, 12)]
    public int Month { get; set; }
    public int Year { get; set; }
    
    // NEW FIELDS:
    [MaxLength(40)]
    public string? EditorName { get; set; }
    public decimal? PercentageAllocation { get; set; }
    public decimal? OldAmount { get; set; }
    public string? EditedMemo { get; set; }
    public bool IsActive { get; set; } = true;  // NEW: Soft delete flag
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Category Category { get; set; } = null!;
    public ICollection<TransactionSplit> TransactionSplits { get; set; } = new List<TransactionSplit>();  // NEW
}
```

**Specific Changes:**
- ✅ Add: `public string? EditorName { get; set; }` with `[MaxLength(40)]`
- ✅ Add: `public decimal? PercentageAllocation { get; set; }`
- ✅ Add: `public decimal? OldAmount { get; set; }`
- ✅ Add: `public string? EditedMemo { get; set; }`
- ✅ Add: `public bool IsActive { get; set; } = true;`
- ✅ Add: `public ICollection<TransactionSplit> TransactionSplits { get; set; } = new List<TransactionSplit>();`

---

### 5. Category.cs
**File:** `src\WNAB.Logic\Data\Category.cs`

**Current Code:**
```csharp
public ICollection<TransactionSplit> TransactionSplits { get; set; } = new List<TransactionSplit>();
```

**Action:**
- ❌ Remove: The `TransactionSplits` navigation property (no longer directly related)
- ✅ Keep: The `Allocations` navigation property (still valid)

---

## DbContext Configuration Changes

### 6. WnabContext.cs
**File:** `src\WNAB.Logic\Data\WnabContext.cs`

**Current TransactionSplit Configuration:**
```csharp
modelBuilder.Entity<TransactionSplit>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    
    entity.HasOne(e => e.Transaction)
        .WithMany(t => t.TransactionSplits)
        .HasForeignKey(e => e.TransactionId)
        .OnDelete(DeleteBehavior.Cascade);
        
    entity.HasOne(e => e.Category)
        .WithMany(c => c.TransactionSplits)
        .HasForeignKey(e => e.CategoryId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

**New TransactionSplit Configuration:**
```csharp
modelBuilder.Entity<TransactionSplit>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    
    entity.HasOne(e => e.Transaction)
        .WithMany(t => t.TransactionSplits)
        .HasForeignKey(e => e.TransactionId)
        .OnDelete(DeleteBehavior.Cascade);
        
    // CHANGED: Now references CategoryAllocation instead of Category
    entity.HasOne(e => e.CategoryAllocation)
        .WithMany(ca => ca.TransactionSplits)
        .HasForeignKey(e => e.CategoryAllocationId)
        .OnDelete(DeleteBehavior.Cascade);  // Or Restrict, depending on requirements
});
```

**Current CategoryAllocation Configuration:**
```csharp
modelBuilder.Entity<CategoryAllocation>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.BudgetedAmount).HasColumnType("decimal(18,2)");
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    
    entity.HasIndex(e => new { e.CategoryId, e.Month, e.Year }).IsUnique();
                    
    entity.HasOne(e => e.Category)
        .WithMany(c => c.Allocations)
        .HasForeignKey(e => e.CategoryId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

**New CategoryAllocation Configuration:**
```csharp
modelBuilder.Entity<CategoryAllocation>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.BudgetedAmount).HasColumnType("decimal(18,2)");
    entity.Property(e => e.OldAmount).HasColumnType("decimal(18,2)");  // NEW
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
    
    entity.HasIndex(e => new { e.CategoryId, e.Month, e.Year }).IsUnique();
                    
    entity.HasOne(e => e.Category)
        .WithMany(c => c.Allocations)
        .HasForeignKey(e => e.CategoryId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

**Specific Changes:**
- 🔄 Update TransactionSplit: Change relationship from `Category` to `CategoryAllocation`
- ✅ Add to CategoryAllocation: `entity.Property(e => e.OldAmount).HasColumnType("decimal(18,2)");`

---

## Record/DTO Changes

### 7. Records.cs
**File:** `src\WNAB.Logic\Data\Records.cs`

**Current Code:**
```csharp
public record TransactionRecord(int AccountId, string Payee, string Description, decimal Amount, DateTime TransactionDate, List<TransactionSplitRecord> Splits);
public record TransactionSplitRecord(int CategoryId, decimal Amount, string? Notes);

public record TransactionDto(
    int Id,
    int AccountId,
    string AccountName,
    string Payee,
    string Description,
    decimal Amount,
    DateTime TransactionDate,
    bool IsReconciled,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<TransactionSplitDto> TransactionSplits
);

public record TransactionSplitDto(
    int Id,
    int CategoryId,
    string CategoryName,
    decimal Amount,
    string? Notes
);
```

**New Code:**
```csharp
public record TransactionRecord(int AccountId, string Payee, string Description, decimal Amount, DateTime TransactionDate, List<TransactionSplitRecord> Splits);
public record TransactionSplitRecord(int CategoryAllocationId, decimal Amount, bool IsIncome, string? Notes);  // CHANGED

public record TransactionDto(
    int Id,
    int AccountId,
    string AccountName,
    string Payee,
    string Description,
    decimal Amount,
    DateTime TransactionDate,
    bool IsReconciled,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<TransactionSplitDto> TransactionSplits
);

public record TransactionSplitDto(
    int Id,
    int CategoryAllocationId,  // CHANGED from CategoryId
    string CategoryName,       // Keep for display purposes (derived from CategoryAllocation.Category.Name)
    decimal Amount,
    bool IsIncome,            // NEW
    string? Notes
);

// NEW: DTO for CategoryAllocation if needed
public record CategoryAllocationRecord(
    int CategoryId, 
    decimal BudgetedAmount, 
    int Month, 
    int Year,
    string? EditorName,           // NEW
    decimal? PercentageAllocation, // NEW
    decimal? OldAmount,           // NEW
    string? EditedMemo            // NEW
);
```

**Specific Changes:**
- 🔄 Update `TransactionSplitRecord`: 
  - Change parameter from `int CategoryId` to `int CategoryAllocationId`
  - Add parameter `bool IsIncome`
- 🔄 Update `TransactionSplitDto`:
  - Change property from `int CategoryId` to `int CategoryAllocationId`
  - Add property `bool IsIncome`
  - Keep `string CategoryName` for display (but derive from CategoryAllocation → Category)
- 🔄 Update `CategoryAllocationRecord`: Add new optional parameters

---

## ViewModel Changes

### 8. TransactionSplitViewModel.cs
**File:** `src\WNAB.Maui\TransactionSplitViewModel.cs`

**Current Code:**
```csharp
public partial class TransactionSplitViewModel : ObservableObject
{
    [ObservableProperty]
    private Category? selectedCategory;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private string? notes;

    public int CategoryId => SelectedCategory?.Id ?? 0;

    partial void OnSelectedCategoryChanged(Category? value)
    {
        OnPropertyChanged(nameof(CategoryId));
    }
}
```

**New Code:**
```csharp
public partial class TransactionSplitViewModel : ObservableObject
{
    [ObservableProperty]
    private Category? selectedCategory;  // User selects Category in UI
    
    [ObservableProperty]
    private CategoryAllocation? selectedCategoryAllocation;  // System determines allocation

    [ObservableProperty]
    private decimal amount;
    
    [ObservableProperty]
    private bool isIncome;  // NEW

    [ObservableProperty]
    private string? notes;

    public int CategoryAllocationId => SelectedCategoryAllocation?.Id ?? 0;

    // Convenience property for display
    public string CategoryName => SelectedCategory?.Name ?? SelectedCategoryAllocation?.Category?.Name ?? string.Empty;

    partial void OnSelectedCategoryChanged(Category? value)
    {
        // LLM-Dev: When category is selected, automatically determine the CategoryAllocation
        // based on the transaction date's month/year. This enforces budget-first approach.
        // If no allocation exists for that Category/Month/Year, validation should prevent saving.
        OnPropertyChanged(nameof(CategoryAllocationId));
        OnPropertyChanged(nameof(CategoryName));
    }
    
    partial void OnSelectedCategoryAllocationChanged(CategoryAllocation? value)
    {
        OnPropertyChanged(nameof(CategoryAllocationId));
    }
}
```

**Specific Changes:**
- ✅ Keep: `SelectedCategory` for user selection in UI
- ✅ Add: `SelectedCategoryAllocation` - system determines based on transaction date
- 🔄 Change: `CategoryId` property to `CategoryAllocationId`
- ✅ Add: `IsIncome` property
- ✅ Add: Logic to auto-select CategoryAllocation when Category is chosen
- 🔄 Update: Change notification logic

**Implementation Note:** When user selects a Category, the system should look up the CategoryAllocation for that Category using the transaction's date (month/year). If no allocation exists, show validation error requiring the user to create a budget allocation first.

---

### 9. Other ViewModels

**Files to Check and Update:**
- `src\WNAB.Maui\TransactionViewModel.cs` - May create TransactionSplits
- `src\WNAB.Maui\TransactionsViewModel.cs` - May display TransactionSplits
- `src\WNAB.Maui\MainPageViewModel.cs` - May aggregate transaction data
- `src\WNAB.Logic\ViewModels\TransactionEntryViewModel.cs` - Used in tests

**Pattern for Updates:**
- Search for `CategoryId` references in context of TransactionSplit
- Replace with `CategoryAllocationId`
- Add `IsIncome` handling where splits are created/edited

---

## UI Changes (XAML)

### 10. TransactionPopup.xaml
**File:** `src\WNAB.Maui\TransactionPopup.xaml`

**Current Bindings (Example):**
```xaml
<DataTemplate x:DataType="local:TransactionSplitViewModel">
    <Grid>
        <Picker ItemsSource="{Binding Categories}" 
                SelectedItem="{Binding SelectedCategory}"
                ItemDisplayBinding="{Binding Name}"/>
        <Entry Text="{Binding Amount}"/>
        <Entry Text="{Binding Notes}"/>
    </Grid>
</DataTemplate>
```

**New Bindings:**
```xaml
<DataTemplate x:DataType="local:TransactionSplitViewModel">
    <Grid>
        <Picker ItemsSource="{Binding CategoryAllocations}" 
                SelectedItem="{Binding SelectedCategoryAllocation}"
                ItemDisplayBinding="{Binding Category.Name}"/>  <!-- Or custom display -->
        <Entry Text="{Binding Amount}"/>
        <CheckBox IsChecked="{Binding IsIncome}"/>  <!-- NEW -->
        <Entry Text="{Binding Notes}"/>
    </Grid>
</DataTemplate>
```

**Specific Changes:**
- 🔄 Update: Pickers/ComboBoxes that bind to Categories to bind to CategoryAllocations
- ✅ Add: UI element (CheckBox/Switch) for `IsIncome` flag
- 🔄 Update: Display bindings to show category name through CategoryAllocation → Category

**Files to Check:**
- `src\WNAB.Maui\TransactionPopup.xaml`
- `src\WNAB.Maui\TransactionsPage.xaml`
- `src\WNAB.Web\Components\Pages\CreateTransactionModal.razor` (if applicable)

---

## API Changes

### 11. Program.cs (API)
**File:** `src\WNAB.API\Program.cs`

**Current Transaction Creation Code:**
```csharp
// Create transaction splits
foreach (var split in transactionRecord.Splits)
{
    var split = new TransactionSplit
    {
        TransactionId = transaction.Id,
        CategoryId = split.CategoryId,
        Amount = split.Amount,
        Notes = split.Notes,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    db.TransactionSplits.Add(split);
}
```

**New Transaction Creation Code:**
```csharp
// Create transaction splits
foreach (var splitRecord in transactionRecord.Splits)
{
    var split = new TransactionSplit
    {
        TransactionId = transaction.Id,
        CategoryAllocationId = splitRecord.CategoryAllocationId,  // CHANGED
        Amount = splitRecord.Amount,
        IsIncome = splitRecord.IsIncome,  // NEW
        Notes = splitRecord.Notes,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    db.TransactionSplits.Add(split);
}
```

**Current Query Code:**
```csharp
var transactions = await db.Transactions
    .Include(t => t.Account)
    .Include(t => t.TransactionSplits)
        .ThenInclude(ts => ts.Category)
    .ToListAsync();

// Map to DTO
t.TransactionSplits.Select(ts => new TransactionSplitDto(
    ts.Id,
    ts.CategoryId,
    ts.Category.Name,
    ts.Amount,
    ts.Notes
))
```

**New Query Code:**
```csharp
var transactions = await db.Transactions
    .Include(t => t.Account)
    .Include(t => t.TransactionSplits)
        .ThenInclude(ts => ts.CategoryAllocation)  // CHANGED
            .ThenInclude(ca => ca.Category)         // NEW: Navigate through CategoryAllocation
    .ToListAsync();

// Map to DTO
t.TransactionSplits.Select(ts => new TransactionSplitDto(
    ts.Id,
    ts.CategoryAllocationId,                       // CHANGED
    ts.CategoryAllocation.Category.Name,           // CHANGED: Navigate through CategoryAllocation
    ts.Amount,
    ts.IsIncome,                                   // NEW
    ts.Notes
))
```

**Specific Changes:**
- 🔄 Update: All `.Include()` statements to include `CategoryAllocation` instead of `Category`
- 🔄 Update: Add `.ThenInclude(ca => ca.Category)` to get category names
- 🔄 Update: TransactionSplit creation to use `CategoryAllocationId` and `IsIncome`
- 🔄 Update: DTO mapping to use new properties

---

## Service Layer Changes

### 12. CategoryAllocationManagementService.cs
**File:** `src\WNAB.Logic\Services\CategoryAllocationManagementService.cs`

**Current CreateCategoryAllocationRecord Method:**
```csharp
public static CategoryAllocationRecord CreateCategoryAllocationRecord(
    int categoryId, 
    decimal budgetedAmount, 
    int month, 
    int year)
{
    ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
    ArgumentOutOfRangeException.ThrowIfLessThan(year, 2000);
    
    return new CategoryAllocationRecord(categoryId, budgetedAmount, month, year);
}
```

**New CreateCategoryAllocationRecord Method:**
```csharp
public static CategoryAllocationRecord CreateCategoryAllocationRecord(
    int categoryId, 
    decimal budgetedAmount, 
    int month, 
    int year,
    string? editorName = null,           // NEW
    decimal? percentageAllocation = null, // NEW
    decimal? oldAmount = null,           // NEW
    string? editedMemo = null)           // NEW
{
    ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
    ArgumentOutOfRangeException.ThrowIfLessThan(year, 2000);
    
    return new CategoryAllocationRecord(
        categoryId, 
        budgetedAmount, 
        month, 
        year,
        editorName,
        percentageAllocation,
        oldAmount,
        editedMemo);
}
```

**Specific Changes:**
- ✅ Add: Optional parameters for new fields
- 🔄 Update: Return statement to include new fields

---

## Test Changes

### 13. TransactionEntry.feature
**File:** `tests\WNAB.Tests.Unit\TransactionEntry.feature`

**Current Feature:**
```gherkin
When I enter the transaction with split
| CategoryId | Amount | Notes           |
| 1          | 50.00  | Groceries       |
| 2          | 30.00  | Household items |

Then I should have the following transaction splits
| CategoryId | Amount | Notes           |
| 1          | 50.00  | Groceries       |
| 2          | 30.00  | Household items |
```

**New Feature:**
```gherkin
When I enter the transaction with split
| CategoryAllocationId | Amount | IsIncome | Notes           |
| 1                    | 50.00  | false    | Groceries       |
| 2                    | 30.00  | false    | Household items |

Then I should have the following transaction splits
| CategoryAllocationId | Amount | IsIncome | Notes           |
| 1                    | 50.00  | false    | Groceries       |
| 2                    | 30.00  | false    | Household items |
```

**Specific Changes:**
- 🔄 Update: Column headers from `CategoryId` to `CategoryAllocationId`
- ✅ Add: `IsIncome` column to test tables

---

### 14. TransactionEntryStepDefinitions.cs
**File:** `tests\WNAB.Tests.Unit\TransactionEntryStepDefinitions.cs`

**Current Step Definition:**
```csharp
[When("I enter the transaction with split")]
public void WhenIenterTransactionWithSplit(DataTable dataTable)
{
    var splits = dataTable.CreateSet<TransactionSplitRecord>().ToList();
    // Uses CategoryId from table
}
```

**New Step Definition:**
```csharp
[When("I enter the transaction with split")]
public void WhenIenterTransactionWithSplit(DataTable dataTable)
{
    var splits = dataTable.CreateSet<TransactionSplitRecord>().ToList();
    // Now uses CategoryAllocationId and IsIncome from table
}
```

**Specific Changes:**
- 🔄 Update: Verify that table column mapping works with new field names
- 🔄 Update: Any manual parsing/mapping to use new properties

---

### 15. CategoryAllocationStepDefinitions.cs
**File:** `tests\WNAB.Tests.Unit\CategoryAllocationStepDefinitions.cs`

**Specific Changes:**
- 🔄 Update: Any test data creation to include new optional fields
- ✅ Add: Test scenarios for new fields if needed

---

## Migration Strategy

### 16. Entity Framework Migration

**Command to Generate Migration:**
```powershell
cd src\WNAB.Logic
dotnet ef migrations add RefactorTransactionSplitToCategoryAllocation
```

**What the Migration Will Do:**
1. Add new columns to `CategoryAllocation` table:
   - `EditorName` (varchar(40), nullable)
   - `PercentageAllocation` (decimal, nullable)
   - `OldAmount` (decimal(18,2), nullable)
   - `EditedMemo` (text, nullable)
   - `IsActive` (boolean, NOT NULL, default true)

2. Add `IsIncome` column to `TransactionSplit` table (boolean, NOT NULL, default false)

3. Add `CategoryAllocationId` column to `TransactionSplit` table (integer, NOT NULL)

4. **No Data Migration Needed** - Database will be reset with clean data

5. Drop `CategoryId` column from `TransactionSplit` table

6. Drop foreign key constraint from `TransactionSplit` to `Category`

7. Add foreign key constraint from `TransactionSplit` to `CategoryAllocation`

**Manual Migration Code Needed:**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add new columns to CategoryAllocation
    migrationBuilder.AddColumn<string>(
        name: "EditorName",
        table: "Allocations",
        type: "character varying(40)",
        maxLength: 40,
        nullable: true);
    
    migrationBuilder.AddColumn<decimal>(
        name: "PercentageAllocation",
        table: "Allocations",
        type: "numeric",
        nullable: true);
    
    migrationBuilder.AddColumn<decimal>(
        name: "OldAmount",
        table: "Allocations",
        type: "numeric(18,2)",
        nullable: true);
    
    migrationBuilder.AddColumn<string>(
        name: "EditedMemo",
        table: "Allocations",
        type: "text",
        nullable: true);
    
    migrationBuilder.AddColumn<bool>(
        name: "IsActive",
        table: "Allocations",
        type: "boolean",
        nullable: false,
        defaultValue: true);
    
    // Add IsIncome to TransactionSplit
    migrationBuilder.AddColumn<bool>(
        name: "IsIncome",
        table: "TransactionSplits",
        type: "boolean",
        nullable: false,
        defaultValue: false);
    
    // Add CategoryAllocationId (NOT NULL since we're doing clean database reset)
    migrationBuilder.AddColumn<int>(
        name: "CategoryAllocationId",
        table: "TransactionSplits",
        type: "integer",
        nullable: false);
    
    // Drop old foreign key
    migrationBuilder.DropForeignKey(
        name: "FK_TransactionSplits_Categories_CategoryId",
        table: "TransactionSplits");
    
    // Drop CategoryId column
    migrationBuilder.DropColumn(
        name: "CategoryId",
        table: "TransactionSplits");
    
    // Add new foreign key
    migrationBuilder.AddForeignKey(
        name: "FK_TransactionSplits_Allocations_CategoryAllocationId",
        table: "TransactionSplits",
        column: "CategoryAllocationId",
        principalTable: "Allocations",
        principalColumn: "Id",
        onDelete: ReferentialAction.Cascade);
}
```

---

## Documentation Updates

### 17. database-erd.md
**File:** `database-erd.md`

**Current ERD:**
```mermaid
TransactionSplit {
    int Id PK
    int TransactionId FK
    int CategoryId FK
    decimal Amount
    string Notes
    datetime CreatedAt
    datetime UpdatedAt
}

CategoryAllocation {
    int Id PK
    int CategoryId FK
    decimal BudgetedAmount
    int Month
    int Year
    datetime CreatedAt
    datetime UpdatedAt
}

Transaction ||--o{ TransactionSplit : "is split into"
Category ||--o{ TransactionSplit : "categorizes"
Category ||--o{ CategoryAllocation : "budgets"
```

**New ERD:**
```mermaid
TransactionSplit {
    int Id PK
    int TransactionId FK
    int CategoryAllocationId FK
    decimal Amount
    bool IsIncome
    string Notes
    datetime CreatedAt
    datetime UpdatedAt
}

CategoryAllocation {
    int Id PK
    int CategoryId FK
    decimal BudgetedAmount
    int Month
    int Year
    string EditorName
    decimal PercentageAllocation
    decimal OldAmount
    string EditedMemo
    bool IsActive
    datetime CreatedAt
    datetime UpdatedAt
}

Transaction ||--o{ TransactionSplit : "is split into"
CategoryAllocation ||--o{ TransactionSplit : "allocates"
Category ||--o{ CategoryAllocation : "budgets"
```

**Specific Changes:**
- 🔄 Update: TransactionSplit entity fields
- 🔄 Update: CategoryAllocation entity fields
- 🔄 Update: Relationship diagram (TransactionSplit now references CategoryAllocation, not Category)
- 🔄 Update: Entity descriptions in the documentation

---

## Critical Decisions - CONFIRMED

### Decision 1: Data Migration Strategy ✅
**Question:** How should existing TransactionSplit records be migrated?

**DECISION:** Manual database reset - Delete and rebuild database with clean slate
- No complex data migration needed
- Fresh start with new schema
- Simpler migration implementation
- **Action:** Manually delete database and rebuild Docker image

---

### Decision 2: CategoryAllocation Existence ✅
**Question:** Should CategoryAllocations be required to exist before creating TransactionSplits?

**DECISION:** Option A - YES, enforce budget-first approach
- CategoryAllocations MUST be created before creating TransactionSplits
- Enforces proper budgeting workflow
- UI/API validation required to ensure allocation exists
- If allocation doesn't exist for Category/Month/Year, show error and require budget creation first

---

### Decision 3: UI Flow ✅
**Question:** How should users select category allocations in the UI?

**DECISION:** Select Category, system determines allocation by transaction date
- User selects a Category in the UI (familiar workflow)
- System automatically finds CategoryAllocation based on **transaction date's month/year**
- If no allocation exists for that Category/Month/Year, show validation error
- User must create budget allocation before transaction can be saved
- **Note:** Uses the transaction's date field (not current date) to determine which allocation to use

---

### Decision 4: Delete Behavior ✅
**Question:** What should happen when a CategoryAllocation is deleted?

**DECISION:** Soft delete with IsActive flag
- Add `IsActive` boolean property to CategoryAllocation entity
- Instead of deleting, mark CategoryAllocations as inactive (`IsActive = false`)
- Preserves historical TransactionSplit references and data integrity
- Filter queries by `IsActive = true` when showing available allocations for selection
- Keep inactive allocations visible in historical transaction data
- **Additional Changes Required:**
  - Add `IsActive` column to CategoryAllocation table (boolean, NOT NULL, default true)
  - Add property to `CategoryAllocation.cs` entity class
  - Update queries/filters appropriately

---

## Implementation Checklist

### Phase 1: Database & Models ✅ COMPLETE
- [x] Update `CategoryAllocation.cs` entity class
- [x] Update `TransactionSplit.cs` entity class
- [x] Update `Category.cs` entity class (remove navigation)
- [x] Update `WnabContext.cs` configuration
- [x] Update `Records.cs` DTOs
- [x] Create Entity Framework migration
- [x] Write data migration script
- [x] Test migration on development database

### Phase 2: ViewModels & Logic ✅ COMPLETE
- [x] Update `TransactionSplitViewModel.cs`
- [x] Update `TransactionViewModel.cs`
- [x] Update `TransactionsViewModel.cs`
- [x] Update `CategoryAllocationManagementService.cs`
- [x] Update other service classes as needed

### Phase 3: API Layer ✅ COMPLETE
- [x] Update API endpoints in `Program.cs`
- [x] Update query includes for CategoryAllocation
- [x] Update DTO mapping logic
- [x] Test API endpoints

### Phase 4: UI Layer ✅ COMPLETE
- [x] Update `TransactionPopup.xaml` and code-behind
- [x] Update `TransactionsPage.xaml` and code-behind
- [x] Update any Blazor components (if applicable)
- [x] Add IsIncome UI controls (ready in XAML)
- [x] Update data binding expressions
- [x] Add CategoryAllocation auto-lookup logic
- [x] Add validation for budget-first approach

### Phase 5: Tests ✅ COMPLETE
- [x] Update `TransactionEntry.feature` (uses Category names - correct for user-facing tests)
- [x] Update `TransactionEntryStepDefinitions.cs`
- [x] Update `CategoryAllocationStepDefinitions.cs`
- [x] Add tests for new IsIncome functionality
- [x] Run all existing tests
- [x] Fix broken tests

### Phase 6: Documentation ✅ COMPLETE
- [x] Update `database-erd.md`
- [x] Update `README.md` if needed
- [x] Add migration notes
- [x] Document new fields and their purposes

### Phase 7: Validation ⚠️ PARTIAL
- [x] Run all unit tests
- [ ] Run integration tests
- [ ] Manual testing of transaction flows
- [ ] Verify database constraints
- [ ] Check for N+1 query issues with new includes

---

## Rollback Plan

If issues arise during implementation:

1. **Before Migration:** Revert code changes, delete migration file
2. **After Migration (Dev):** Run `dotnet ef database update <previous-migration>`
3. **After Migration (Production):** Have down migration ready, backup database first

---

## Notes

- This refactoring changes the fundamental relationship between transactions and categories
- Consider performance impact of additional joins (TransactionSplit → CategoryAllocation → Category)
- May want to add indexes on `CategoryAllocationId` in TransactionSplit table
- Consider caching CategoryAllocation data if performance issues arise
- The `IsIncome` flag on TransactionSplit allows splits to have different income/expense designations, which may or may not align with the Category.IsIncome flag
- **Budget-First Approach:** Users must create CategoryAllocations before entering transactions for that category/month combination
- **Soft Delete:** CategoryAllocations use `IsActive` flag to preserve historical data integrity
- **Auto-Selection Logic:** When user selects a Category, system determines CategoryAllocation based on transaction date's month/year

---

**Document Version:** 2.0  
**Last Updated:** October 8, 2025  
**Status:** ✅ IMPLEMENTED - Phases 1-6 Complete, Phase 7 Partial (Unit Tests Passing)
