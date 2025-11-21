using Microsoft.AspNetCore.Authentication;
using WNAB.Web.Services;

namespace WNAB.Web;

public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthenticationDelegatingHandler> _logger;
    private readonly WebAuthenticationService _authService;

    public AuthenticationDelegatingHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthenticationDelegatingHandler> logger,
        WebAuthenticationService authService)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            // Use the authentication service to refresh token if needed
            await _authService.RefreshTokenIfNeededAsync();

            // Get the (possibly refreshed) access token
            var accessToken = await httpContext.GetTokenAsync("access_token");

            if (!string.IsNullOrEmpty(accessToken))
            {
                // Add the access token to the Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                _logger.LogTrace("Added Bearer token to request: {Url}", request.RequestUri);
            }
            else
            {
                _logger.LogWarning("No access token available for request: {Url}", request.RequestUri);
            }
        }
        else
        {
            _logger.LogWarning("No HttpContext available for request: {Url}", request.RequestUri);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Request failed with status {Status} for {Url}", response.StatusCode, request.RequestUri);
        }

        return response;
    }
}
