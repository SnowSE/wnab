using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using WNAB.Logic;
using WNAB.Logic.Data;
using WNAB.Maui.Services;

namespace WNAB.Maui;

// LLM-Dev: TransactionsViewModel to display transactions for a user across all their accounts
public partial class TransactionsViewModel : ObservableObject
{
    private readonly TransactionManagementService _transactions;
    private readonly IPopupService _popupService;
    private readonly IAuthenticationService _authService;

    public ObservableCollection<TransactionItem> Items { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private string statusMessage = "Loading...";

    public TransactionsViewModel(TransactionManagementService transactions, IPopupService popupService, IAuthenticationService authService)
    {
        _transactions = transactions;
        _popupService = popupService;
        _authService = authService;
    }

    // LLM-Dev: Initialize by checking user session and loading transactions
    public async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadTransactionsAsync();
        }
    }

    // LLM-Dev: Check if user is logged in using AuthenticationService
    [RelayCommand]
    private async Task CheckUserSessionAsync()
    {
        try
        {
            IsLoggedIn = await _authService.IsAuthenticatedAsync();
            if (IsLoggedIn)
            {
                var userName = await _authService.GetUserNameAsync();
                StatusMessage = $"Logged in as {userName ?? "user"}";
            }
            else
            {
                IsLoggedIn = false;
                StatusMessage = "Please log in to view transactions";
                Items.Clear();
            }
        }
        catch
        {
            IsLoggedIn = false;
            StatusMessage = "Error checking login status";
            Items.Clear();
        }
    }

    // LLM-Dev:v2 Load transactions for the current authenticated user (now using DTOs)
    [RelayCommand]
    private async Task LoadTransactionsAsync()
    {
        if (IsBusy || !IsLoggedIn) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Loading transactions...";
            Items.Clear();

            var list = await _transactions.GetTransactionsForUserAsync();
            foreach (var t in list)
            {
                // LLM-Dev:v2 DTO now has CategoryName directly in TransactionSplits
                var categoryNames = t.TransactionSplits.Select(ts => ts.CategoryName ?? "Unknown").ToList();
                var categoriesText = categoryNames.Count > 1
                    ? $"{categoryNames.Count} categories"
                    : categoryNames.FirstOrDefault() ?? "No category";

                Items.Add(new TransactionItem(
                    t.Id,
                    t.TransactionDate,
                    t.Payee,
                    t.Description,
                    t.Amount,
                    t.AccountName, // DTO has AccountName directly
                    categoriesText));
            }

            StatusMessage = list.Count == 0 ? "No transactions found" : $"Loaded {list.Count} transactions";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading transactions: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // LLM-Dev: Refresh command for manual reload
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await CheckUserSessionAsync();
        if (IsLoggedIn)
        {
            await LoadTransactionsAsync();
        }
    }

    // LLM-Dev: Add command to open Add Transaction popup
    [RelayCommand]
    private async Task AddTransaction()
    {
        await _popupService.ShowNewTransactionAsync();
        // Refresh the list after popup closes
        await RefreshAsync();
    }

    // LLM-Dev:v2 Navigation command to return to home page
    [RelayCommand]
    private async Task NavigateToHome()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}

// LLM-Dev: Item model for displaying transaction information in the UI
public sealed record TransactionItem(
    int Id, 
    DateTime Date, 
    string Payee, 
    string Description, 
    decimal Amount, 
    string AccountName,
    string Categories);