using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace WNAB.MVM;

public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthenticationDelegatingHandler> _logger;

    public AuthenticationDelegatingHandler(IAuthenticationService authService, ILogger<AuthenticationDelegatingHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await _authService.GetAccessTokenAsync();

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _logger.LogTrace("Added Bearer token to request: {Url}", request.RequestUri);
        }
        else
        {
            _logger.LogWarning("No access token available for request: {Url}", request.RequestUri);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Request failed with status {Status} for {Url}", response.StatusCode, request.RequestUri);
        }

        return response;
    }
}
