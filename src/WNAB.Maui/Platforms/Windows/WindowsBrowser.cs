using IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace WNAB.Maui.Platforms.Windows;

public class WindowsBrowser : IdentityModel.OidcClient.Browser.IBrowser
{
    private readonly int _port;
    private readonly ILogger _logger;

    public WindowsBrowser(ILogger logger, int port = 0)
    {
        _logger = logger;
        _port = port;
    }

    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        using var listener = new HttpListener();

        // Use a random port if not specified
        var port = _port == 0 ? GetRandomUnusedPort() : _port;
        var redirectUri = $"http://localhost:{port}/";

        listener.Prefixes.Add(redirectUri);

        try
        {
            listener.Start();

            // Replace the redirect_uri in the StartUrl with our actual port
            var startUrl = options.StartUrl;
            _logger.LogInformation("Original StartUrl: {StartUrl}", startUrl);
            _logger.LogInformation("Redirect URI with port: {RedirectUri}", redirectUri);

            if (startUrl.Contains("redirect_uri="))
            {
                // Find and replace the redirect_uri parameter value
                var redirectUriParam = "redirect_uri=";
                var startIndex = startUrl.IndexOf(redirectUriParam);
                if (startIndex >= 0)
                {
                    startIndex += redirectUriParam.Length;
                    var endIndex = startUrl.IndexOf('&', startIndex);
                    if (endIndex < 0) endIndex = startUrl.Length;

                    var oldRedirectUri = startUrl.Substring(startIndex, endIndex - startIndex);
                    startUrl = startUrl.Replace(
                        redirectUriParam + oldRedirectUri,
                        redirectUriParam + Uri.EscapeDataString(redirectUri)
                    );
                }
            }

            _logger.LogInformation("Modified StartUrl: {StartUrl}", startUrl);

            // Open the browser with the modified start URL
            OpenBrowser(startUrl);

            // Wait for the callback
            var context = await listener.GetContextAsync();

            var response = context.Response;
            var responseString = "<html><head><meta http-equiv='refresh' content='10;url=https://engineering.snow.edu'></head><body>Please return to the app.</body></html>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer, 0, buffer.Length);
            responseOutput.Close();

            var url = context.Request.Url?.ToString();

            if (string.IsNullOrEmpty(url))
            {
                return new BrowserResult
                {
                    ResultType = BrowserResultType.UnknownError,
                    Error = "No URL received"
                };
            }

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
        finally
        {
            listener.Stop();
        }
    }

    private static int GetRandomUnusedPort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // Workaround for .NET Core issue on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else
            {
                throw;
            }
        }
    }
}
