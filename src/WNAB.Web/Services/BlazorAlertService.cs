using Microsoft.JSInterop;

namespace WNAB.Web.Services;

public class BlazorAlertService : WNAB.MVM.IAlertService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<BlazorAlertService> _logger;
    public BlazorAlertService(IJSRuntime jsRuntime, ILogger<BlazorAlertService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task DisplayAlertAsync(string title, string message)
    {
        _logger.LogDebug("DisplayAlertAsync called with title: {Title}", title);
        await _jsRuntime.InvokeVoidAsync("alert", $"{title}\n\n{message}");
    }

    public async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
    {
        _logger.LogDebug("DisplayAlertAsync (confirm) called with title: {Title}", title);
        return await _jsRuntime.InvokeAsync<bool>("confirm", $"{title}\n\n{message}");
    }
}