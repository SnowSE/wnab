using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Text.Json.Serialization;

namespace WNAB.Web.Services;

/// <summary>
/// Blazor Web implementation of WNAB.MVM.IAuthenticationService.
/// Uses HttpContext and ASP.NET Core authentication instead of MAUI's SecureStorage.
/// </summary>
public class WebAuthenticationService : WNAB.MVM.IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<WebAuthenticationService> _logger;
    private readonly IConfiguration _configuration;
    private static readonly HttpClient _httpClient = new HttpClient();

    public WebAuthenticationService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<WebAuthenticationService> logger,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _configuration = configuration;
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
            _logger.LogTrace("User is already authenticated");
            return true;
        }

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

    /// <summary>
    /// Checks if the current access token is expired by parsing the JWT and checking the 'exp' claim.
    /// Automatically attempts to refresh the token if it's expired or expiring soon.
    /// </summary>
    /// <returns>True if token is expired or invalid and refresh failed, false if token is still valid or was successfully refreshed.</returns>
    public async Task<bool> IsTokenExpiredAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("No HttpContext available to check token expiration");
            return true;
        }

        // First, try to refresh the token if needed
        await RefreshTokenIfNeededAsync();

        // After potential refresh, check the token again
        var accessToken = await GetAccessTokenAsync();

        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("No access token available to check expiration");
            return true; // Treat missing token as expired
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);

            // Get the expiration claim
            var expiration = token.ValidTo;

            // Check if token is expired (with a small buffer of 30 seconds)
            var isExpired = expiration <= DateTime.UtcNow.AddSeconds(30);

            if (isExpired)
            {
                _logger.LogTrace("Access token is still expired after refresh attempt. Expiration: {Expiration}, Current: {Current}",
                    expiration, DateTime.UtcNow);
            }

            return isExpired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token expiration");
            return true; // Treat invalid token as expired
        }
    }

    /// <summary>
    /// Checks if the token needs refresh and attempts to refresh it if necessary.
    /// </summary>
    public async Task RefreshTokenIfNeededAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        try
        {
            // Check if token needs refresh using the expires_at property
            var expiresAt = await httpContext.GetTokenAsync("expires_at");
            var refreshToken = await httpContext.GetTokenAsync("refresh_token");

            if (string.IsNullOrEmpty(expiresAt))
            {
                _logger.LogWarning("No expires_at token found in authentication properties");
                return;
            }

            var expiresAtDate = DateTimeOffset.Parse(expiresAt, CultureInfo.InvariantCulture);

            // Refresh if token expires in less than 1 minute
            if (expiresAtDate < DateTimeOffset.UtcNow.AddMinutes(1))
            {
                _logger.LogInformation("Access token expired or expiring soon, attempting refresh...");

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await RefreshTokenAsync(httpContext, refreshToken);
                }
                else
                {
                    _logger.LogWarning("No refresh token available, user may need to re-authenticate");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if token needs refresh");
        }
    }

    /// <summary>
    /// Refreshes the access token using the refresh token.
    /// </summary>
    private async Task RefreshTokenAsync(HttpContext httpContext, string refreshToken)
    {
        try
        {
            var keycloakConfig = _configuration.GetSection("Keycloak");
            var authority = keycloakConfig["Authority"];
            var clientId = keycloakConfig["ClientId"];
            var clientSecret = keycloakConfig["ClientSecret"];

            var tokenEndpoint = $"{authority}/protocol/openid-connect/token";

            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!
            };

            var response = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));

            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();

                if (payload != null)
                {
                    var expiresAt = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn);

                    // Update the tokens in the authentication properties
                    var authenticateResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    if (authenticateResult.Succeeded)
                    {
                        authenticateResult.Properties.UpdateTokenValue("access_token", payload.AccessToken);
                        authenticateResult.Properties.UpdateTokenValue("refresh_token", payload.RefreshToken ?? refreshToken);
                        authenticateResult.Properties.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));

                        // Re-sign in to update the cookie
                        await httpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            authenticateResult.Principal!,
                            authenticateResult.Properties);

                        _logger.LogInformation("Successfully refreshed access token");
                    }
                }
            }
            else
            {
                _logger.LogError("Failed to refresh token: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
        }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
