using System.Net.Http.Json;
using WNAB.Logic.Data;

namespace WNAB.Logic;

public class AccountManagementService
{
	private readonly HttpClient _http;

	public AccountManagementService(HttpClient http)
	{
		_http = http ?? throw new ArgumentNullException(nameof(http));
	}

	public static AccountRecord CreateAccountRecord(string name, int userId)
	{
		if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Account name required", nameof(name));
		if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId), "UserId must be positive.");
		return new AccountRecord(name, userId);
	}


	public async Task<int> CreateAccountAsync(int userId, AccountRecord record, CancellationToken ct = default)
	{

		// LLM-Dev:v2 Use POST to the REST endpoint that accepts userId as query parameter and AccountRecord in body.
		var response = await _http.PostAsJsonAsync($"accounts?userId={userId}", record, ct);
		response.EnsureSuccessStatusCode();

		var created = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
		if (created is null) throw new InvalidOperationException("API returned no content when creating account.");
		return created.Id;
	}

	private sealed record IdResponse(int Id);


	public async Task<List<Account>> GetAccountsForUserAsync(int userId, CancellationToken ct = default)
	{
		if (userId <= 0) return new();
		var list = await _http.GetFromJsonAsync<List<Account>>($"accounts?userId={userId}", ct);
		return list ?? new();
	}
}