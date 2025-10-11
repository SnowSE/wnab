using Microsoft.EntityFrameworkCore;
using WNAB.Data;

namespace WNAB.API.Services;

public class UserProvisioningService
{
    private readonly WnabContext _context;

    public UserProvisioningService(WnabContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets or creates a user based on Keycloak subject ID and claims
    /// </summary>
    public async Task<User> GetOrCreateUserAsync(string subjectId, string email, string? firstName, string? lastName)
    {
        // Try to find existing user by Keycloak subject ID
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.KeycloakSubjectId == subjectId);

        if (user != null)
        {
            // Update user information if changed
            bool hasChanges = false;

            if (user.Email != email)
            {
                user.Email = email;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(firstName) && user.FirstName != firstName)
            {
                user.FirstName = firstName;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(lastName) && user.LastName != lastName)
            {
                user.LastName = lastName;
                hasChanges = true;
            }

            if (hasChanges)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return user;
        }

        // Create new user
        user = new User
        {
            KeycloakSubjectId = subjectId,
            Email = email,
            FirstName = firstName ?? email.Split('@')[0], // Use email prefix if no first name
            LastName = lastName ?? "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }
}
