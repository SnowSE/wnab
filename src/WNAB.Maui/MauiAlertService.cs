namespace WNAB.Maui;

public class MauiAlertService : IAlertService
{
    public async Task DisplayAlertAsync(string title, string message)
    {
        await Shell.Current.DisplayAlertAsync(title, message, "OK");
    }

    public async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
    {
        return await Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
    }
}