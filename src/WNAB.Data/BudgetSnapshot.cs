using System;
using System.Collections.Generic;
using System.Text;

namespace WNAB.Data;

public class BudgetSnapshot
{
    public int Id { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal SnapshotReadyToAssign { get; set; }
    public List<CategorySnapshotData> Categories { get; set; } = new();
}

public class CategorySnapshotData
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal AssignedValue { get; set; }
    public decimal Activity { get; set; }
    public decimal Available { get; set; }
}
