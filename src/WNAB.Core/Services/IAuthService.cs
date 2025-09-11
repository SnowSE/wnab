using WNAB.Core.Models;

namespace WNAB.Core.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string firstName, string lastName, string email, string password);
    Task<AuthResult> LoginAsync(string email, string password);
    Task<bool> UserExistsAsync(string email);
}

public class AuthResult
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public User? User { get; set; }
    public string Error { get; set; } = string.Empty;
}