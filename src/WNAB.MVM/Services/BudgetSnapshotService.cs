using System.Net.Http.Json;
using WNAB.Data;

namespace WNAB.MVM;

public class BudgetSnapshotService : IBudgetSnapshotService
{
    private readonly HttpClient _httpClient;

    public BudgetSnapshotService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BudgetSnapshot?> GetSnapshotAsync(int month, int year)
    {
        try
        {
            var response = await _httpClient.GetAsync($"budget/snapshot?month={month}&year={year}");
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<BudgetSnapshot>();
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveSnapshotAsync(BudgetSnapshot snapshot)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("budget/snapshot", snapshot);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            // Log or handle error
            throw new InvalidOperationException($"Failed to save snapshot: {ex.Message}", ex);
        }
    }
}
