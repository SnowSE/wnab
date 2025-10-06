using Microsoft.AspNetCore.Authentication;

namespace WNAB.Web;

public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthenticationDelegatingHandler> _logger;

    public AuthenticationDelegatingHandler(IHttpContextAccessor httpContextAccessor, ILogger<AuthenticationDelegatingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            // Get the access token from the authenticated user
            var accessToken = await httpContext.GetTokenAsync("access_token");

            if (!string.IsNullOrEmpty(accessToken))
            {
                // Add the access token to the Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                _logger.LogInformation("Added Bearer token to request: {Url}", request.RequestUri);
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
