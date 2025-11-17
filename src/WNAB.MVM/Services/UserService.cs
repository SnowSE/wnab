using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace WNAB.MVM;

public class UserService(HttpClient _http) : IUserService
{

    public async Task<DateTime> GetEarliestActivityDate() => await _http.GetFromJsonAsync<DateTime>("user/earliestActivity");

}
