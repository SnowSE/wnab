using System.Net.Http.Json;
using WNAB.Data;

namespace WNAB.Services;


public class UserManagementService
{
	private readonly HttpClient _http;


	public UserManagementService(HttpClient http)
	{
		_http = http ?? throw new ArgumentNullException(nameof(http));
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

    // to do, make an endpoint to get user by id, this works but is inefficient
    public async Task<User?> GetUserByIdAsync(int userId, CancellationToken ct = default)
	{
		var users = await GetUsersAsync(ct);
		return users.FirstOrDefault(u => u.Id == userId && u.IsActive);
	}
}
