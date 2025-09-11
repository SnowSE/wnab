using Microsoft.AspNetCore.Mvc;
using WNAB.Core.Services;

namespace WNAB.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(request.FirstName, request.LastName, request.Email, request.Password);

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new
        {
            token = result.Token,
            user = new
            {
                id = result.User!.Id,
                firstName = result.User.FirstName,
                lastName = result.User.LastName,
                email = result.User.Email
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.Success)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(new
        {
            token = result.Token,
            user = new
            {
                id = result.User!.Id,
                firstName = result.User.FirstName,
                lastName = result.User.LastName,
                email = result.User.Email
            }
        });
    }
}

public class RegisterRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}