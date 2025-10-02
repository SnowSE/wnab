namespace WNAB.Maui.Services;

public interface IAuthenticationService
{
    Task<bool> LoginAsync();
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    string? GetUserName();
}
