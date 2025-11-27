using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shelter.Application.Auth.Contracts;
using Shelter.Application.Interfaces;
using Shelter.Domain.Users;

namespace Shelter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService, UserManager<User> userManager) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await authService.LoginAsync(request, cancellationToken);

        if (result is null)
            return Unauthorized(new { message = "Invalid credentials." });

        return Ok(result);
    }

    // POST: /api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            // Set Index
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            // auto login after registration for now. Eventually add email confirmation step
            return await Login(new LoginRequest { Email = user.Email, Password = request.Password }, cancellationToken);
        }
        
        var errors = result.Errors.Select(e => e.Description).ToArray();
        return BadRequest(new { errors });
    }
    
    [HttpGet("debug/auth")]
    [AllowAnonymous]
    public IActionResult DebugAuth()
    {
        return Ok(new
        {
            User.Identity?.IsAuthenticated,
            User.Identity?.AuthenticationType,
            Schemes = HttpContext.RequestServices
                .GetRequiredService<IAuthenticationSchemeProvider>()
                .GetAllSchemesAsync().Result.Select(s => s.Name)
        });
    }

    
    [HttpGet(Name = "TestAuth")]
    [Authorize]
    public IActionResult TestAuth()
    {
        return Ok("Logged in!");
    }
    
    // Add refresh, register, logout endpoints later as needed
}