using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WNAB.Maui;

public class AuthenticationService : IAuthenticationService
{
    private readonly OidcClient _oidcClient;
    private LoginResult? _loginResult;
    private string? _currentAccessToken;
    private DateTimeOffset _tokenExpiration;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(IConfiguration configuration, ILogger<AuthenticationService> logger)
    {
        _logger = logger;
        var authority = configuration["Keycloak:Authority"] ?? "https://engineering.snow.edu/auth/realms/SnowCollege";
        var clientId = configuration["Keycloak:ClientId"] ?? "wnab-maui";

        // Use different redirect URIs based on platform
        string redirectUri;
        IdentityModel.OidcClient.Browser.IBrowser browser;
                
#if WINDOWS
        // For Windows, create the browser first to get the actual redirect URI with port
        var windowsBrowser = new Platforms.Windows.WindowsBrowser(logger);
        redirectUri = windowsBrowser.RedirectUri;
        browser = windowsBrowser;
#else
        redirectUri = configuration["Keycloak:RedirectUri"] ?? "wnab://callback";
        browser = new WebBrowserAuthenticator();
#endif

        var options = new OidcClientOptions
        {
            Authority = authority,
            ClientId = clientId,
            Scope = "openid profile email offline_access",
            RedirectUri = redirectUri,
            Browser = browser,
            Policy = new Policy
            {
                RequireIdentityTokenSignature = false
            }
        };

        _oidcClient = new OidcClient(options);
    }

    public async Task<bool> LoginAsync()
    {
        try
        {
            var loginRequest = new LoginRequest();
            _loginResult = await _oidcClient.LoginAsync(loginRequest);

            if (_loginResult.IsError)
            {
                _logger.LogError("Login error: {Error}, Description: {ErrorDescription}", _loginResult.Error, _loginResult.ErrorDescription);
                return false;
            }

            _logger.LogTrace("Login successful!");

            // Store current token info
            _currentAccessToken = _loginResult.AccessToken;
            _tokenExpiration = _loginResult.AccessTokenExpiration;

            // Store tokens securely
            await SecureStorage.SetAsync("access_token", _loginResult.AccessToken);
            await SecureStorage.SetAsync("refresh_token", _loginResult.RefreshToken ?? string.Empty);
            await SecureStorage.SetAsync("id_token", _loginResult.IdentityToken);

            _logger.LogTrace("Tokens stored securely");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login exception occurred: {Message}", ex.Message);
            if (ex.InnerException != null)
            {
                _logger.LogError(ex.InnerException, "Inner exception: {InnerMessage}", ex.InnerException.Message);
            }
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
          // Simple logout - just clear local tokens
            // (No need to call server logout which opens a browser)
          
            // Clear stored tokens from SecureStorage
    SecureStorage.Remove("access_token");
            SecureStorage.Remove("refresh_token");
SecureStorage.Remove("id_token");

            // Clear in-memory state
            _loginResult = null;
     _currentAccessToken = null;
   _tokenExpiration = DateTimeOffset.MinValue;

            _logger.LogTrace("User logged out successfully (local tokens cleared)");
        }
        catch (Exception ex)
        {
         _logger.LogError(ex, "Logout exception: {Message}", ex.Message);
          // Even if there's an error, try to clear what we can
    _loginResult = null;
            _currentAccessToken = null;
    _tokenExpiration = DateTimeOffset.MinValue;
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            // First try to get from memory
            if (!string.IsNullOrEmpty(_currentAccessToken))
            {
                // Check if token is expired (with 1 minute buffer)
                if (_tokenExpiration > DateTimeOffset.UtcNow.AddMinutes(1))
                {
                    return _currentAccessToken;
                }

                _logger.LogTrace("Access token expired or expiring soon, attempting refresh...");
            }

            // Try to refresh token if expired or not in memory
            var refreshToken = _loginResult?.RefreshToken ?? await SecureStorage.GetAsync("refresh_token");
            if (!string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogTrace("Attempting to refresh access token...");
                var refreshResult = await _oidcClient.RefreshTokenAsync(refreshToken);
                if (!refreshResult.IsError)
                {
                    _logger.LogTrace("Successfully refreshed access token");

                    // Update current token and expiration
                    _currentAccessToken = refreshResult.AccessToken;
                    _tokenExpiration = refreshResult.AccessTokenExpiration;

                    // Store updated tokens
                    await SecureStorage.SetAsync("access_token", refreshResult.AccessToken);
                    await SecureStorage.SetAsync("refresh_token", refreshResult.RefreshToken ?? string.Empty);
                    await SecureStorage.SetAsync("id_token", refreshResult.IdentityToken);

                    return refreshResult.AccessToken;
                }
                else
                {
                    _logger.LogWarning("Failed to refresh token: {Error}", refreshResult.Error);
                }
            }

            // If refresh failed or no refresh token, try to get from secure storage
            var storedToken = await SecureStorage.GetAsync("access_token");
            if (!string.IsNullOrEmpty(storedToken))
            {
                _currentAccessToken = storedToken;

                // Try to parse expiration from the token
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.ReadJwtToken(storedToken);
                    _tokenExpiration = token.ValidTo;

                    // Check if stored token is still valid
                    if (_tokenExpiration > DateTimeOffset.UtcNow.AddMinutes(1))
                    {
                        return storedToken;
                    }
                    else
                    {
                        _logger.LogWarning("Stored access token is expired and refresh failed");
                        return null;
                    }
                }
                catch
                {
                    // If we can't parse, assume it's valid for now
                    _logger.LogWarning("Could not parse token expiration from stored token");
                }
            }
            return storedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get token exception: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetUserNameAsync()
    {
        var accessToken = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);

            // Try to get the 'name' claim
            var nameClaim = token.Claims.FirstOrDefault(c => c.Type == "name");
            return nameClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading username from access token");
            return null;
        }
    }

    private class WebBrowserAuthenticator : IdentityModel.OidcClient.Browser.IBrowser
    {
        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            try
            {
                var callbackUrl = new Uri(options.EndUrl);
                var result = await WebAuthenticator.Default.AuthenticateAsync(
                    new Uri(options.StartUrl),
                    callbackUrl);

                var url = new UriBuilder(callbackUrl)
                {
                    Query = string.Join("&", result.Properties.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"))
                }.ToString();

                return new BrowserResult
                {
                    Response = url,
                    ResultType = BrowserResultType.Success
                };
            }
            catch (TaskCanceledException)
            {
                return new BrowserResult
                {
                    ResultType = BrowserResultType.UserCancel
                };
            }
            catch (Exception ex)
            {
                return new BrowserResult
                {
                    ResultType = BrowserResultType.UnknownError,
                    Error = ex.Message
                };
            }
        }
    }
}
