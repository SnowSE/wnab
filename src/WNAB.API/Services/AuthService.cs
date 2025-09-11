using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WNAB.Core.Models;
using WNAB.Core.Services;
using WNAB.Data;

namespace WNAB.API.Services;

public class AuthService : IAuthService
{
    private readonly WnabDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(WnabDbContext context, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<AuthResult> RegisterAsync(string firstName, string lastName, string email, string password)
    {
        if (await UserExistsAsync(email))
        {
            return new AuthResult { Success = false, Error = "User already exists with this email address" };
        }

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new AuthResult 
        { 
            Success = true, 
            Token = token, 
            User = user 
        };
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        
        if (user == null)
        {
            return new AuthResult { Success = false, Error = "Invalid email or password" };
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        
        if (result == PasswordVerificationResult.Failed)
        {
            return new AuthResult { Success = false, Error = "Invalid email or password" };
        }

        var token = GenerateJwtToken(user);

        return new AuthResult 
        { 
            Success = true, 
            Token = token, 
            User = user 
        };
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"] ?? "your-secret-key"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"] ?? "WNAB",
            audience: _configuration["JWT:Audience"] ?? "WNAB-Users",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}