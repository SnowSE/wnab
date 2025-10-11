namespace WNAB.MVM;

public interface IAuthenticationService
{
    Task<bool> LoginAsync();
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetUserNameAsync();
}
