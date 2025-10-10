using System;
using System.Collections.Generic;
using System.Text;

namespace WNAB.Maui.NewMainPage;

// give me all the allocations!
record CategoryAllocationRequest(int CategoryId);

// here are the allocations:
record CategoryAllocationResponse(List<Allocation> allocations);
record Allocation(int Id, decimal amount);

// give me all the transactions!
record AllocationTransactionsRequest(int Id);

// here are the transactions:
record AllocationTransactionsResponse(List<TransactionSplits> transactionsplits);
record TransactionSplits(int Id, int CategoryAllocationId, int TransactionId, decimal amount);


record AllocationChangeRequest(int Id, decimal amount);

// return a bool?
record CategoryAllocChangeResponse();

record CategoryAllocDeleteRequest(int Id);

record CategoryAllocDeleteResponse();
