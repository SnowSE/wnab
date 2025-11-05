using System.Net.Http.Json;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

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
		
		// Check for bad request and extract the error message from the API
		if (!response.IsSuccessStatusCode)
		{
			if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
			{
				try
				{
					var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
					throw new InvalidOperationException(errorResponse?.Error ?? "Invalid account data");
				}
				catch (System.Text.Json.JsonException)
				{
					// If the error response isn't JSON with Error property, read as string
					var errorText = await response.Content.ReadAsStringAsync(ct);
					throw new InvalidOperationException(errorText);
				}
			}
			// For other errors, use default behavior
			response.EnsureSuccessStatusCode();
		}

		var created = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
		if (created is null) throw new InvalidOperationException("API returned no content when creating account.");
		return created.Id;
	}

	private sealed record IdResponse(int Id);
	private sealed record ErrorResponse(string Error);

	// LLM-Dev:v2 Fetch accounts for the current authenticated user (UI should call this rather than creating HttpClient).
	public async Task<List<Account>> GetAccountsForUserAsync(CancellationToken ct = default)
	{
		var list = await _http.GetFromJsonAsync<List<Account>>("accounts", ct);
		return list ?? new();
	}

	/// <summary>
	/// Update an existing account's name and type for the current authenticated user.
	/// Returns true on success, false if account not found or update failed.
	/// </summary>
	public async Task<bool> UpdateAccountAsync(int accountId, string newName, AccountType newAccountType, CancellationToken ct = default)
	{
		var request = new EditAccountRequest(accountId, newName, newAccountType);
		var response = await _http.PutAsJsonAsync($"accounts/{accountId}", request, ct);
		
		return response.IsSuccessStatusCode;
	}

	/// <summary>
	/// Delete an account for the current authenticated user.
	/// Returns true on success, false if account not found or delete failed.
	/// </summary>
	public async Task<bool> DeleteAccountAsync(int accountId, CancellationToken ct = default)
	{
		var response = await _http.DeleteAsync($"accounts/{accountId}", ct);
		return response.IsSuccessStatusCode;
	}

	/// <summary>
	/// Get inactive accounts for the current authenticated user.
	/// Returns a list of inactive accounts.
	/// </summary>
	public async Task<List<Account>> GetInactiveAccountsAsync(CancellationToken ct = default)
	{
		var list = await _http.GetFromJsonAsync<List<Account>>("accounts/inactive", ct);
		return list ?? new();
	}

	/// <summary>
	/// Reactivate an inactive account for the current authenticated user.
	/// Returns a tuple with success status and error message (if any).
	/// </summary>
	public async Task<(bool Success, string? ErrorMessage)> ReactivateAccountAsync(int accountId, CancellationToken ct = default)
	{
		var response = await _http.PutAsync($"accounts/{accountId}/reactivate", null, ct);
		
		if (response.IsSuccessStatusCode)
		{
			return (true, null);
		}

		// Extract error message from response
		string? errorMessage = null;
		try
		{
			if (response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
				response.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				// These return plain string error messages
				errorMessage = await response.Content.ReadAsStringAsync(ct);
			}
			else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
			{
				// Conflict returns JSON with error property
				var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
				errorMessage = errorResponse?.Error;
			}
			else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
			{
				errorMessage = "You don't have permission to reactivate this account.";
			}
			else
			{
				errorMessage = $"Failed to reactivate account. Status: {response.StatusCode}";
			}
		}
		catch
		{
			errorMessage = "Failed to reactivate account. Unable to read error details.";
		}

		return (false, errorMessage);
	}
}