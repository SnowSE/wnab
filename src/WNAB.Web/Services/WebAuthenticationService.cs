using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;

namespace WNAB.Web.Services;

/// <summary>
/// Blazor Web implementation of WNAB.MVM.IAuthenticationService.
/// Uses HttpContext and ASP.NET Core authentication instead of MAUI's SecureStorage.
/// </summary>
public class WebAuthenticationService : WNAB.MVM.IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<WebAuthenticationService> _logger;

    public WebAuthenticationService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<WebAuthenticationService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// In Blazor Web, login is handled by OpenIdConnect middleware at /login endpoint.
    /// This method checks if the user is already authenticated.
    /// </summary>
    public async Task<bool> LoginAsync()
    {
        // In Web apps, login is handled by middleware redirecting to /login
        // This method just checks if we're already authenticated
        var isAuthenticated = await IsAuthenticatedAsync();
        
        if (isAuthenticated)
        {
            _logger.LogInformation("User is already authenticated");
            return true;
        }
        
        _logger.LogInformation("User is not authenticated. Login must be initiated via /login endpoint.");
        return false;
    }

    /// <summary>
    /// Logout is handled by authentication middleware at /logout endpoint.
    /// This method is a no-op as the actual logout happens via the endpoint.
    /// </summary>
    public Task LogoutAsync()
    {
        _logger.LogInformation("Logout requested. Actual logout handled by middleware at /logout endpoint.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the access token from the current HttpContext.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("No HttpContext available");
            return null;
        }

        try
        {
            var accessToken = await httpContext.GetTokenAsync("access_token");
            
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("No access token found in HttpContext");
            }
            
            return accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access token");
            return null;
        }
    }

    /// <summary>
    /// Checks if the current user is authenticated.
    /// </summary>
    public Task<bool> IsAuthenticatedAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Task.FromResult(false);
        }

        var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated ?? false;
        return Task.FromResult(isAuthenticated);
    }

    /// <summary>
    /// Gets the username from the current authenticated user's claims.
    /// </summary>
    public Task<string?> GetUserNameAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            // Try to get username from claims
            var userName = httpContext.User?.Identity?.Name;
            
            if (string.IsNullOrEmpty(userName))
            {
                // Try preferred_username claim (Keycloak standard)
                userName = httpContext.User?.Claims
                    .FirstOrDefault(c => c.Type == "preferred_username")?.Value;
            }

            if (string.IsNullOrEmpty(userName))
            {
                // Try name claim
                userName = httpContext.User?.Claims
                    .FirstOrDefault(c => c.Type == "name")?.Value;
            }

            return Task.FromResult<string?>(userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving username");
            return Task.FromResult<string?>(null);
        }
    }
}
