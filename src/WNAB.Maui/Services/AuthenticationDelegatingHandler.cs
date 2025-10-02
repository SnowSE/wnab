using System.Net.Http.Headers;

namespace WNAB.Maui.Services;

public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly IAuthenticationService _authService;

    public AuthenticationDelegatingHandler(IAuthenticationService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await _authService.GetAccessTokenAsync();

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
