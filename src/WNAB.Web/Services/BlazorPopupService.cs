namespace WNAB.Web.Services;

/// <summary>
/// Blazor implementation of IMVMPopupService.
/// In Blazor, modals are handled declaratively via Bootstrap modals in Razor components,
/// so this service is a lightweight stub that doesn't do anything.
/// The actual modal triggering is done via data-bs-toggle or JavaScript in the Razor pages.
/// </summary>
public class BlazorPopupService : WNAB.MVM.IMVMPopupService
{
    private readonly ILogger<BlazorPopupService> _logger;

    public BlazorPopupService(ILogger<BlazorPopupService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// In Blazor, transactions modal is triggered via Bootstrap data attributes in the UI.
    /// This method is called by ViewModels but doesn't need to do anything.
    /// </summary>
    public Task ShowNewTransactionAsync()
    {
        _logger.LogDebug("ShowNewTransactionAsync called - Blazor handles modals declaratively");
        return Task.CompletedTask;
    }

    /// <summary>
    /// In Blazor, category modal is triggered via Bootstrap data attributes in the UI.
    /// This method is called by ViewModels but doesn't need to do anything.
    /// </summary>
    public Task ShowAddCategoryAsync()
    {
        _logger.LogDebug("ShowAddCategoryAsync called - Blazor handles modals declaratively");
        return Task.CompletedTask;
    }

    /// <summary>
    /// In Blazor, edit category modal is triggered via Bootstrap data attributes in the UI.
    /// This method is called by ViewModels but doesn't need to do anything.
    /// </summary>
    public Task ShowEditCategoryAsync(int categoryId, string name, string? color, bool isActive)
    {
        _logger.LogDebug("ShowEditCategoryAsync called for category {CategoryId} - Blazor handles modals declaratively", categoryId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// In Blazor, user modal is triggered via Bootstrap data attributes in the UI.
    /// This method is called by ViewModels but doesn't need to do anything.
    /// </summary>
    public Task ShowAddUserAsync()
    {
        _logger.LogDebug("ShowAddUserAsync called - Blazor handles modals declaratively");
        return Task.CompletedTask;
    }

    /// <summary>
    /// In Blazor, account modal is triggered via Bootstrap data attributes in the UI.
    /// This method is called by ViewModels but doesn't need to do anything.
    /// </summary>
    public Task ShowAddAccountAsync()
    {
        _logger.LogDebug("ShowAddAccountAsync called - Blazor handles modals declaratively");
        return Task.CompletedTask;
    }

   
}
