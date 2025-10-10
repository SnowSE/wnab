# Plan Budget - Save Implementation Plan

**Date**: October 10, 2025  
**Feature**: Complete the Budget Planning page to save CategoryAllocations  
**Current Status**: UI displays categories and accepts budget amounts, but Save button doesn't create allocations

---

## Current State Analysis

### ✅ What's Working
1. **UI Layout**: Categories panel (left), main content area (center), toolbar (bottom)
2. **Category Selection**: Users can select categories from the left panel
3. **Budget Entry**: Selected categories display with numeric input fields
4. **Data Binding**: Budget amounts are bound to the UI inputs
5. **Service Registration**: `CategoryAllocationManagementService` is already registered in DI

### ❌ What's Missing
1. **Month/Year Selection**: No UI elements to specify which month/year the budget is for
2. **Save Implementation**: The `SaveAsync()` method only shows a placeholder message
3. **Loading Existing Allocations**: No way to view/edit existing budgets for a month
4. **Validation**: No validation for duplicate allocations or invalid inputs
5. **Error Handling**: No user feedback for API errors

---

## Required Changes

### 1. Add Month/Year Selection UI

**File**: `src/WNAB.Web/Components/Pages/PlanBudget.razor`

**Location**: Top of the main content area (before the selected categories list)

**UI Elements Needed**:
- Month dropdown (1-12) or month picker
- Year input field (numeric, default to current year)
- Display format: "Budget for [Month] [Year]" as a header

**Implementation Details**:
```
- Add private fields in @code section:
  - int _selectedMonth (default to current month)
  - int _selectedYear (default to current year)
  
- Add UI section after the "Display Categories" button area:
  - Month selector (dropdown or <select>)
    - Options: January (1) through December (12)
    - Bind to _selectedMonth
  - Year input (number input)
    - Bind to _selectedYear
    - Min: 2020, Max: 2099
    
- Consider placing this in a card/section at the top of the main content area
- Should be visible even when no categories are selected
```

**Visual Placement**:
```
┌─────────────────────────────────────────┐
│  Budget for: [October ▼] [2025]        │  <- NEW SECTION
├─────────────────────────────────────────┤
│  ┌──────────────────────────────────┐  │
│  │ food                         [X] │  │
│  │ Budget Amount: $ [200]           │  │
│  └──────────────────────────────────┘  │
│                                         │
│  [Cancel]                      [Save]  │
└─────────────────────────────────────────┘
```

---

### 2. Implement Save Functionality

**File**: `src/WNAB.Web/Components/Pages/PlanBudget.razor`

**Changes to @code section**:

#### 2.1 Inject the Service
```csharp
@inject CategoryAllocationManagementService AllocationService
```

**Note**: Service is already registered in `Program.cs` (line 89-90), so just need to inject it.

#### 2.2 Replace the `SaveAsync()` Method

**Current placeholder code (line 286-292)**:
```csharp
private Task SaveAsync()
{
    // TODO: Implement save logic when budget plan service is available
    _statusMessage = "Save functionality coming soon...";
    StateHasChanged();
    return Task.CompletedTask;
}
```

**New implementation needed**:
```csharp
private async Task SaveAsync()
{
    // Validation
    - Check if user is logged in
    - Check if month/year are valid
    - Check if any categories are selected
    - Validate budget amounts (>= 0)
    
    // Set loading state
    - _isLoading = true
    - _statusMessage = "Saving budget allocations..."
    - StateHasChanged()
    
    // Save each category allocation
    - Loop through _selectedCategories
    - For each category:
      - Create CategoryAllocationRecord using static factory
      - Call AllocationService.CreateCategoryAllocationAsync()
      - Handle success/errors
    
    // Handle results
    - Track successes and failures
    - Display appropriate messages
    - On success: clear selected categories or navigate away
    - On error: show specific error messages
    
    // Reset loading state
    - _isLoading = false
    - StateHasChanged()
}
```

#### 2.3 Detailed Save Logic Flow

**Pseudo-code**:
```
1. Pre-validation:
   - if (!_isLoggedIn) → show error "Please log in"
   - if (_selectedCategories.Count == 0) → show error "No categories selected"
   - if (_selectedMonth < 1 || _selectedMonth > 12) → show error "Invalid month"
   - if (_selectedYear < 2020) → show error "Invalid year"

2. Input validation:
   - foreach category in _selectedCategories:
     - if (category.BudgetAmount < 0) → show error "Budget amount must be >= 0"

3. Save process:
   - Create list to track results
   - Set _isLoading = true, _error = null
   - _statusMessage = "Saving budget allocations..."
   
   - foreach category in _selectedCategories:
     a. Create record:
        var record = CategoryAllocationManagementService.CreateCategoryAllocationRecord(
            category.Id,
            category.BudgetAmount,
            _selectedMonth,
            _selectedYear
        );
     
     b. Try to save:
        try {
            var id = await AllocationService.CreateCategoryAllocationAsync(record);
            // Track success
        }
        catch (HttpRequestException ex) {
            // Likely a duplicate allocation (unique constraint violation)
            // Track error with category name
        }
        catch (Exception ex) {
            // Other errors
            // Track error
        }

4. Display results:
   - if (all successful):
     - _statusMessage = "Successfully saved X budget allocations!"
     - Clear selected categories
     - Option: Navigate to home or stay on page
   
   - if (some failed):
     - _error = "Failed to save some allocations: [list details]"
     - Keep failed categories in the list
     - Remove successful ones
   
   - if (all failed):
     - _error = "Failed to save budget allocations. Please try again."

5. Cleanup:
   - _isLoading = false
   - StateHasChanged()
```

---

### 3. Handle Duplicate Allocations

**Issue**: The database has a unique constraint on `(CategoryId, Month, Year)`. If a user tries to create an allocation that already exists, the API will return an error.

**Solutions**:

#### Option A: Check Before Saving (Recommended)
```
- Before saving, call API to check if allocation exists:
  - For each category, call AllocationService.FindAllocationAsync(categoryId, month, year)
  - If allocation exists:
    - Option 1: Show error "Budget for [Category] in [Month Year] already exists"
    - Option 2: Ask user if they want to update/overwrite (requires UPDATE endpoint)
  - If not exists, proceed with create
```

#### Option B: Handle Error After Save Attempt
```
- Try to save
- If HttpRequestException with 400/409 status:
  - Parse error message
  - Show user-friendly message: "Budget for [Category] in [Month Year] already exists"
  - Suggest viewing existing budgets or choosing a different month
```

**Recommendation**: Use Option A for better UX (prevent the error before it happens)

---

### 4. Load Existing Allocations (Optional but Recommended)

**Purpose**: Allow users to view and edit existing budgets instead of only creating new ones.

**Implementation**:

#### 4.1 Add "Load Existing Budget" Feature
```
- When month/year changes:
  - Trigger LoadExistingAllocationsAsync()
  - For each user's category:
    - Call AllocationService.FindAllocationAsync(categoryId, _selectedMonth, _selectedYear)
    - If allocation exists:
      - Add to _selectedCategories with existing BudgetAmount
      - Remove from _categories list

- This pre-populates the budget page with existing data
```

#### 4.2 Update vs Create Logic
```
- Need to track which allocations are new vs existing
- Add property to BudgetCategoryItem: int? AllocationId
  - null = new allocation (use POST)
  - has value = existing allocation (would need PUT endpoint)

- Note: Current API only has POST, no PUT/PATCH for updates
- For Phase 1: Only allow creating new allocations
- For Phase 2: Add update endpoint and support editing
```

---

### 5. API Considerations

**Current Endpoint**: `POST /categories/allocation`

**Location**: `src/WNAB.API/Program.cs` (lines 243-256)

**What it does**:
- Accepts `CategoryAllocationRecord`
- Creates new `CategoryAllocation` entity
- Saves to database
- Returns `Created` with allocation ID

**What it doesn't handle**:
- Checking for duplicates (relies on database constraint)
- Updating existing allocations
- Soft deletes (IsActive flag is not set)

**Potential Issues**:
1. **Duplicate Constraint**: Will throw exception if (CategoryId, Month, Year) already exists
2. **Missing Fields**: API endpoint doesn't set:
   - `EditorName` (from CategoryAllocationRecord)
   - `PercentageAllocation` (from CategoryAllocationRecord)
   - `OldAmount` (from CategoryAllocationRecord)
   - `EditedMemo` (from CategoryAllocationRecord)
   - `IsActive` (defaults to false, should be true)

**Recommended API Fix**:
```csharp
// Current code (lines 244-252):
var allocation = new CategoryAllocation
{
    CategoryId = rec.CategoryId,
    BudgetedAmount = rec.BudgetedAmount,
    Month = rec.Month,
    Year = rec.Year
};

// Should be:
var allocation = new CategoryAllocation
{
    CategoryId = rec.CategoryId,
    BudgetedAmount = rec.BudgetedAmount,
    Month = rec.Month,
    Year = rec.Year,
    EditorName = rec.EditorName,
    PercentageAllocation = rec.PercentageAllocation,
    OldAmount = rec.OldAmount,
    EditedMemo = rec.EditedMemo,
    IsActive = true  // Important: mark as active
};
```

---

### 6. Error Handling & User Feedback

**Error Scenarios to Handle**:

1. **No categories selected**
   - Show: "Please select at least one category to budget"

2. **Invalid budget amount**
   - Show: "Budget amounts must be 0 or greater"

3. **Network error**
   - Show: "Unable to connect to server. Please check your connection."

4. **Duplicate allocation (409 Conflict)**
   - Show: "Budget for [Category] in [Month Year] already exists. Choose a different month or edit the existing budget."

5. **Unauthorized (401)**
   - Show: "Your session has expired. Please log in again."
   - Redirect to login page

6. **Category doesn't exist (404)**
   - Show: "Category not found. Please refresh the page."

**Success Feedback**:
- Show success message with count: "Successfully saved 3 budget allocations!"
- Option to:
  - Return to home
  - Start budgeting for another month
  - View created allocations

---

## Implementation Checklist

### Phase 1: Basic Save Functionality
- [ ] Add month/year selection UI elements
  - [ ] Month dropdown (1-12)
  - [ ] Year number input
  - [ ] Default to current month/year
  - [ ] Add header showing selected period

- [ ] Inject CategoryAllocationManagementService
  - [ ] Add @inject directive

- [ ] Implement SaveAsync() method
  - [ ] Add validation for logged in user
  - [ ] Add validation for selected categories
  - [ ] Add validation for month/year
  - [ ] Add validation for budget amounts
  - [ ] Loop through selected categories
  - [ ] Create CategoryAllocationRecord for each
  - [ ] Call API via AllocationService
  - [ ] Handle success/error responses
  - [ ] Update UI with results

- [ ] Add error handling
  - [ ] Try-catch blocks for API calls
  - [ ] User-friendly error messages
  - [ ] Handle duplicate constraint violations

- [ ] Add success feedback
  - [ ] Show success message
  - [ ] Clear selected categories (optional)
  - [ ] Option to navigate away

### Phase 2: Enhanced Features (Optional)
- [ ] Load existing allocations
  - [ ] Add LoadExistingAllocationsAsync() method
  - [ ] Call when month/year changes
  - [ ] Pre-populate budget amounts

- [ ] Update API endpoint
  - [ ] Fix missing fields (EditorName, IsActive, etc.)
  - [ ] Add PUT endpoint for updates
  - [ ] Add duplicate check before insert

- [ ] Edit existing budgets
  - [ ] Track AllocationId in BudgetCategoryItem
  - [ ] Differentiate create vs update
  - [ ] Call appropriate endpoint

- [ ] Bulk operations
  - [ ] "Copy from previous month" feature
  - [ ] "Clear all" button
  - [ ] "Apply percentage increase" feature

---

## Testing Plan

### Manual Testing
1. **Happy Path**:
   - Select month/year
   - Select 2-3 categories
   - Enter budget amounts
   - Click Save
   - Verify success message
   - Check database for created allocations

2. **Validation**:
   - Try to save without selecting categories
   - Try to save with negative amounts
   - Try to save without logging in

3. **Duplicate Handling**:
   - Create an allocation
   - Try to create same allocation again
   - Verify appropriate error message

4. **Network Errors**:
   - Stop the API
   - Try to save
   - Verify error handling

### Database Verification
```sql
-- Check created allocations
SELECT * FROM "Allocations" 
WHERE "Month" = 10 AND "Year" = 2025
ORDER BY "CategoryId";

-- Verify unique constraint
SELECT "CategoryId", "Month", "Year", COUNT(*) 
FROM "Allocations" 
GROUP BY "CategoryId", "Month", "Year" 
HAVING COUNT(*) > 1;
```

---

## Files to Modify

### Required Changes
1. **src/WNAB.Web/Components/Pages/PlanBudget.razor**
   - Add month/year UI elements
   - Inject CategoryAllocationManagementService
   - Implement SaveAsync() method
   - Add error handling and validation

### Recommended Changes
2. **src/WNAB.API/Program.cs**
   - Fix CategoryAllocation creation to include all fields
   - Set IsActive = true

### Optional Changes
3. **src/WNAB.Logic/Services/CategoryAllocationManagementService.cs**
   - Add method to load allocations for a specific month/year
   - Add method to check if allocation exists before creating

---

## Notes & Considerations

### Month/Year UX Design
- **Current Month Default**: Most users budget for the current or upcoming month
- **Easy Navigation**: Consider prev/next month buttons
- **Visual Clarity**: Display the selected period prominently

### Error Messages
- Be specific about what went wrong
- Provide actionable guidance (e.g., "try a different month")
- Don't expose technical details to users

### Performance
- Saving 10-20 categories = 10-20 individual API calls
- Consider: Bulk save endpoint if performance becomes an issue
- For now, sequential saves are fine for typical use

### Data Integrity
- The unique constraint prevents duplicate allocations (good)
- IsActive flag allows "soft deletes" for historical data
- Consider: Should we allow "editing" by creating new allocation and marking old one inactive?

### Future Enhancements
- Visual budget summary (total budgeted, by category)
- Budget vs. Actual comparison
- Budget templates (copy from previous month)
- Percentage-based budgeting
- Budget goals and progress tracking

---

## Risk Assessment

### Low Risk
- Adding UI elements for month/year
- Basic validation
- Injecting existing service

### Medium Risk
- API error handling (need to handle various HTTP status codes)
- Duplicate allocation scenarios
- State management (when to clear selected categories)

### High Risk
- Database unique constraint violations (could confuse users)
  - **Mitigation**: Pre-check with FindAllocationAsync before saving
- Missing fields in API (EditorName, IsActive not being set)
  - **Mitigation**: Fix API endpoint to set all fields

---

## Estimated Effort

### Phase 1 (Basic Save)
- UI Changes: 1-2 hours
- Save Logic: 2-3 hours
- Testing: 1-2 hours
- **Total**: 4-7 hours

### Phase 2 (Enhanced Features)
- Load Existing: 2-3 hours
- API Updates: 1-2 hours
- Update Logic: 2-3 hours
- Testing: 1-2 hours
- **Total**: 6-10 hours

### Grand Total: 10-17 hours

---

## Summary

The Plan Budget page is **90% complete** from a UI perspective. The missing pieces are:

1. **Month/Year Selection** (simple UI addition)
2. **Save Implementation** (core logic to create CategoryAllocations)
3. **Error Handling** (robust error messages and validation)

Once these are implemented, users will be able to:
- ✅ View their categories
- ✅ Select categories to budget
- ✅ Enter budget amounts
- ✅ **Choose which month/year** (NEW)
- ✅ **Save the budget plan** (NEW)
- ✅ **Get feedback on success/errors** (NEW)

The foundation is solid. The service layer and API endpoints exist. We just need to wire up the UI to call them properly.
