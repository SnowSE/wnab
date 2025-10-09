using IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace WNAB.Maui.Platforms.Windows;

public class WindowsBrowser : IdentityModel.OidcClient.Browser.IBrowser
{
    private readonly int _port;
    private readonly ILogger _logger;
    public string RedirectUri { get; }

    public WindowsBrowser(ILogger logger, int port = 0)
    {
        _logger = logger;
        _port = port == 0 ? GetRandomUnusedPort() : port;
        RedirectUri = $"http://localhost:{_port}/";
        _logger.LogInformation("WindowsBrowser initialized with RedirectUri: {RedirectUri}", RedirectUri);
    }

    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        // Use TcpListener instead of HttpListener to avoid HTTP.sys request length limits
        var tcpListener = new TcpListener(IPAddress.Loopback, _port);

        try
        {
            tcpListener.Start();
            _logger.LogInformation("TCP listener started on {RedirectUri}", RedirectUri);

            // Replace the redirect_uri in the StartUrl with our actual port
            var startUrl = options.StartUrl;
            _logger.LogInformation("OAuth StartUrl (original): {StartUrl}", startUrl);
            _logger.LogInformation("OAuth EndUrl: {EndUrl}", options.EndUrl);
            _logger.LogInformation("Using RedirectUri: {RedirectUri}", RedirectUri);

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
                        redirectUriParam + Uri.EscapeDataString(RedirectUri)
                    );
                }
            }

            _logger.LogInformation("Modified StartUrl: {StartUrl}", startUrl);

            // Open the browser with the modified start URL
            _logger.LogInformation("Opening system browser...");
            OpenBrowser(startUrl);

            // Wait for the callback
            _logger.LogInformation("Waiting for OAuth callback...");
            var client = await tcpListener.AcceptTcpClientAsync();
            _logger.LogInformation("Callback received!");

            string callbackUrl = "";

            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                // Read the HTTP request line
                var requestLine = await reader.ReadLineAsync();
                _logger.LogInformation("Request line: {RequestLine}", requestLine);

                if (requestLine != null && requestLine.StartsWith("GET"))
                {
                    // Extract the path and query string from the request line
                    var parts = requestLine.Split(' ');
                    if (parts.Length >= 2)
                    {
                        var pathAndQuery = parts[1];
                        callbackUrl = RedirectUri.TrimEnd('/') + pathAndQuery;
                        _logger.LogInformation("Extracted callback URL: {CallbackUrl}", callbackUrl);
                    }
                }

                // Read and discard the rest of the headers
                string? line;
                while ((line = await reader.ReadLineAsync()) != null && !string.IsNullOrWhiteSpace(line))
                {
                    // Just consume the headers
                }

                // Send a simple HTTP response
                await writer.WriteLineAsync("HTTP/1.1 200 OK");
                await writer.WriteLineAsync("Content-Type: text/html");
                await writer.WriteLineAsync("Connection: close");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("<html><body><h1>Success!</h1><p>You can close this window and return to the app.</p></body></html>");
            }

            client.Close();

            if (string.IsNullOrEmpty(callbackUrl))
            {
                _logger.LogError("Failed to extract callback URL from request");
                return new BrowserResult
                {
                    ResultType = BrowserResultType.UnknownError,
                    Error = "Failed to extract callback URL"
                };
            }

            _logger.LogInformation("Returning success with Response URL: {Url}", callbackUrl);
            return new BrowserResult
            {
                Response = callbackUrl,
                ResultType = BrowserResultType.Success
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "OAuth flow was cancelled by user");
            return new BrowserResult
            {
                ResultType = BrowserResultType.UserCancel
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth browser flow: {Message}", ex.Message);
            return new BrowserResult
            {
                ResultType = BrowserResultType.UnknownError,
                Error = ex.Message
            };
        }
        finally
        {
            tcpListener.Stop();
            _logger.LogInformation("TCP listener stopped");
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
