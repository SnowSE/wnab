using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace WNAB.Maui.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly OidcClient _oidcClient;
    private LoginResult? _loginResult;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(IConfiguration configuration, ILogger<AuthenticationService> logger)
    {
        _logger = logger;
        var authority = configuration["Keycloak:Authority"] ?? "https://engineering.snow.edu/auth/realms/SnowCollege";
        var clientId = configuration["Keycloak:ClientId"] ?? "wnab-maui";

        // Use different redirect URIs based on platform
#if WINDOWS
        // For Windows, we'll use a dynamic port that the browser will determine
        var redirectUri = "http://localhost/";
#else
        var redirectUri = configuration["Keycloak:RedirectUri"] ?? "wnab://callback";
#endif

        var options = new OidcClientOptions
        {
            Authority = authority,
            ClientId = clientId,
            Scope = "openid profile email",
            RedirectUri = redirectUri,
#if WINDOWS
            Browser = new Platforms.Windows.WindowsBrowser(logger),
#else
            Browser = new WebBrowserAuthenticator(),
#endif
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
            _logger.LogInformation("Starting login with redirect URI: {RedirectUri}", _oidcClient.Options.RedirectUri);

            _loginResult = await _oidcClient.LoginAsync(loginRequest);

            if (_loginResult.IsError)
            {
                _logger.LogError("Login error: {Error}, Description: {ErrorDescription}", _loginResult.Error, _loginResult.ErrorDescription);
                return false;
            }

            // Store tokens securely
            await SecureStorage.SetAsync("access_token", _loginResult.AccessToken);
            await SecureStorage.SetAsync("refresh_token", _loginResult.RefreshToken ?? string.Empty);
            await SecureStorage.SetAsync("id_token", _loginResult.IdentityToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login exception occurred");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            if (_loginResult != null)
            {
                await _oidcClient.LogoutAsync(new LogoutRequest
                {
                    IdTokenHint = _loginResult.IdentityToken
                });
            }

            // Clear stored tokens
            SecureStorage.Remove("access_token");
            SecureStorage.Remove("refresh_token");
            SecureStorage.Remove("id_token");

            _loginResult = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout exception: {ex.Message}");
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            // First try to get from memory
            if (_loginResult != null && !string.IsNullOrEmpty(_loginResult.AccessToken))
            {
                // Check if token is expired
                if (_loginResult.AccessTokenExpiration > DateTime.UtcNow)
                {
                    return _loginResult.AccessToken;
                }

                // Try to refresh token
                if (!string.IsNullOrEmpty(_loginResult.RefreshToken))
                {
                    var refreshResult = await _oidcClient.RefreshTokenAsync(_loginResult.RefreshToken);
                    if (!refreshResult.IsError)
                    {
                        // Store the refreshed login result
                        _loginResult = await _oidcClient.LoginAsync(new LoginRequest());

                        await SecureStorage.SetAsync("access_token", refreshResult.AccessToken);
                        await SecureStorage.SetAsync("refresh_token", refreshResult.RefreshToken ?? string.Empty);
                        await SecureStorage.SetAsync("id_token", refreshResult.IdentityToken);

                        return refreshResult.AccessToken;
                    }
                }
            }

            // Try to get from secure storage
            var storedToken = await SecureStorage.GetAsync("access_token");
            return storedToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get token exception: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public string? GetUserName()
    {
        return _loginResult?.User?.Identity?.Name;
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
