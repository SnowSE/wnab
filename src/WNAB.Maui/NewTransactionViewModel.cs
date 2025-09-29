using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.Maui;

public partial class NewTransactionViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private string memo = string.Empty;

    public event EventHandler? RequestClose; // Raised to close popup

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task Save()
    {
        // TODO: Persist transaction via injected service when available
        if (Amount <= 0) return;
        RequestClose?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }
}