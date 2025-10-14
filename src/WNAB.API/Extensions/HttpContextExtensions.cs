using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WNAB.Data;

namespace WNAB.API.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the current authenticated user from the JWT token.
    /// Automatically provisions the user if they don't exist in the database.
    /// </summary>
    public static async Task<User?> GetCurrentUserAsync(this HttpContext context, WnabContext db, Services.UserProvisioningService provisioningService)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Services.UserProvisioningService>>();
        
        // Log all claims in the token for debugging
        logger.LogInformation("=== JWT Token Claims ===");
        foreach (var claim in context.User.Claims)
        {
            logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
        }
        logger.LogInformation("=== End Claims ===");
        
        // Try "cid" claim first (Snow College), then fall back to standard "sub" claim
        var subjectId = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (string.IsNullOrEmpty(subjectId))
        {
            logger.LogWarning("No 'sub' claim found in token. Available claims logged above.");
            return null;
        }

        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
            ?? context.User.FindFirst("email")?.Value
            ?? context.User.FindFirst("preferred_username")?.Value
            ?? "unknown@example.com";
        var firstName = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?.Value
            ?? context.User.FindFirst("given_name")?.Value;
        var lastName = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")?.Value
            ?? context.User.FindFirst("family_name")?.Value;

        return await provisioningService.GetOrCreateUserAsync(subjectId, email, firstName, lastName);
    }
}
