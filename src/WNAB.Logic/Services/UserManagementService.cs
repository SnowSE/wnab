using System.Net.Http.Json;
using WNAB.Logic.Data;

namespace WNAB.Logic;

/// <summary>
/// Handles user-related operations via the API.
/// </summary>
public class UserManagementService
{
	private readonly HttpClient _http;


	public UserManagementService(HttpClient http)
	{
		_http = http ?? throw new ArgumentNullException(nameof(http));
	}


	public static UserRecord CreateUserRecord(string firstName, string lastName, string email)
	{
		if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name required", nameof(firstName));
		if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name required", nameof(lastName));
		if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email required", nameof(email));
		return new UserRecord(firstName, lastName, email);
	}

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


	public async Task<List<User>> GetUsersAsync(CancellationToken ct = default)
	{
		var users = await _http.GetFromJsonAsync<List<User>>("all/users", ct);
		return users ?? new();
	}

	public async Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default)
	{
		var users = await GetUsersAsync(ct);
		return users.FirstOrDefault(u => u.Id == userId && u.IsActive);
	}
}
