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
            System.Diagnostics.Debug.WriteLine($"[BudgetSnapshotService] Requesting snapshot for {month}/{year}");
            
            var response = await _httpClient.GetAsync($"budget/snapshot/{month}/{year}");
            
            System.Diagnostics.Debug.WriteLine($"[BudgetSnapshotService] Response status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[BudgetSnapshotService] Error response: {errorContent}");
                return null;
            }

            var snapshot = await response.Content.ReadFromJsonAsync<BudgetSnapshot>();
            System.Diagnostics.Debug.WriteLine($"[BudgetSnapshotService] Deserialized snapshot: {(snapshot == null ? "NULL" : $"RTA={snapshot.SnapshotReadyToAssign}, Categories={snapshot.Categories?.Count ?? 0}")} ");
            return snapshot;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BudgetSnapshotService] Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BudgetSnapshotService] Stack: {ex.StackTrace}");
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

    public async Task InvalidateSnapshotsFromMonthAsync(int month, int year)
    {
        try
        {
            var response = await _httpClient.PostAsync($"budget/snapshot/invalidate?month={month}&year={year}", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to invalidate snapshots: {ex.Message}", ex);
        }
    }
}
