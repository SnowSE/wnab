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

	/// <summary>
	/// Create an account for the current authenticated user by POSTing to the API.
	/// Returns the newly created account Id on success.
	/// </summary>
	public async Task<int> CreateAccountAsync(AccountRecord record, CancellationToken ct = default)
	{

		// LLM-Dev: Use POST to the REST endpoint that accepts AccountRecord for current user.
		var response = await _http.PostAsJsonAsync("accounts", record, ct);
		response.EnsureSuccessStatusCode();

		var created = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
		if (created is null) throw new InvalidOperationException("API returned no content when creating account.");
		return created.Id;
	}

	private sealed record IdResponse(int Id);

	// LLM-Dev:v2 Fetch accounts for the current authenticated user (UI should call this rather than creating HttpClient).
	public async Task<List<Account>> GetAccountsForUserAsync(CancellationToken ct = default)
	{
		var list = await _http.GetFromJsonAsync<List<Account>>("accounts", ct);
		return list ?? new();
	}
}