using System;
using System.Collections.Generic;
using System.Text;

namespace WNAB.Maui.NewMainPageViewModels;

// give me all the allocations!
public record CategoryAllocationsRequest(int CategoryId);

// here are the allocations:
public record CategoryAllocationResponse(List<Allocation> allocations);
public record Allocation(int Id, decimal amount);

// give me all the transactions!
public record AllocationTransactionsRequest(int Id);

// here are the transactions:
public record AllocationTransactionsResponse(List<TransactionSplits> transactionsplits);
public record TransactionSplits(int Id, int CategoryAllocationId, int TransactionId, decimal amount);


public record AllocationChangeRequest(int Id, decimal amount);

// return a bool?
public record CategoryAllocChangeResponse();

public record CategoryAllocDeleteRequest(int Id);

public record CategoryAllocDeleteResponse();
