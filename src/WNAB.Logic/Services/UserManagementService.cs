using System.Net.Http.Json;
using WNAB.Logic.Data;

namespace WNAB.Logic;

/// <summary>
/// Handles user-related operations via the API.
/// </summary>
public class UserManagementService
{
	private readonly HttpClient _http;

	/// <summary>
	/// Construct with an HttpClient configured with API BaseAddress (e.g. https://localhost:7077/)
	/// </summary>
	public UserManagementService(HttpClient http)
	{
		_http = http ?? throw new ArgumentNullException(nameof(http));
	}

	// LLM-Dev: Split into two methods as requested: one to create the DTO, one to send it to the API.
	/// <summary>
	/// Creates a <see cref="UserRecord"/> DTO from inputs.
	/// </summary>
	public static UserRecord CreateUserRecord(string name, string email)
	{
		if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
		if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email required", nameof(email));
		return new UserRecord(name, email);
	}

	/// <summary>
	/// Sends the provided <see cref="UserRecord"/> to the API via POST /users and returns the created user Id.
	/// </summary>
	public async Task<int> CreateUserAsync(UserRecord record, CancellationToken ct = default)
	{
		if (record is null) throw new ArgumentNullException(nameof(record));
		var response = await _http.PostAsJsonAsync("users", record, ct);
		response.EnsureSuccessStatusCode();

		var created = await response.Content.ReadFromJsonAsync<UserCreatedResponse>(cancellationToken: ct);
		if (created is null) throw new InvalidOperationException("API returned no content when creating user.");
		return created.Id;
	}

	private sealed record UserCreatedResponse(int Id, string FirstName, string LastName, string Email);

	// LLM-Dev:v2 Add list method so UI does not create HttpClients directly.
	public async Task<List<User>> GetUsersAsync(CancellationToken ct = default)
	{
		var users = await _http.GetFromJsonAsync<List<User>>("users", ct);
		return users ?? new();
	}
}
