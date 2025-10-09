using System;
using System.Collections.Generic;
using System.Text;

namespace WNAB.Maui.NewMainPage;

record CategoryAllocationResponse(int Id, int CategoryId);
record CategoryAllocationRequest(int CategoryId, decimal amount, );
