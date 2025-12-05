namespace WNAB.MVM;

/// <summary>
/// Interface for displaying alerts/dialogs to the user.
/// </summary>
public interface IAlertService
{
    Task DisplayAlertAsync(string title, string message);
    Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel);
}
