using System;
using System.Collections.Generic;
using System.Text;

namespace WNAB.MVM;

public interface IUserService
{
    public Task<DateTime> GetEarliestActivityDate();
    public Task<int> GetUserId();

}
