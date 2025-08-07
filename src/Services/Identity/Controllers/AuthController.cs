using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Identity.Api.Models;
using Shared.Common.Models;
using MassTransit;
using Shared.Contracts.Events;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IPublishEndpoint publishEndpoint,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse>> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            WalletAddress = request.WalletAddress,
            UserType = request.UserType
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            // Publish user registered event
            await _publishEndpoint.Publish(new UserRegistered(
                Guid.Parse(user.Id),
                user.Email!,
                user.UserType.ToString(),
                DateTime.UtcNow
            ));

            _logger.LogInformation("User {Email} registered successfully", user.Email);
            return Ok(ApiResponse.SuccessResult("User registered successfully"));
        }

        var errors = result.Errors.Select(e => e.Description).ToList();
        return BadRequest(ApiResponse.ErrorResult("Registration failed", errors));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResult("Invalid credentials"));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (result.Succeeded)
        {
            // Generate JWT token here (implement JWT generation logic)
            var token = "generated-jwt-token"; // Placeholder
            
            var response = new LoginResponse
            {
                Token = token,
                UserId = Guid.Parse(user.Id),
                Email = user.Email!,
                UserType = user.UserType.ToString()
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Login successful"));
        }

        return Unauthorized(ApiResponse<LoginResponse>.ErrorResult("Invalid credentials"));
    }
}

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? WalletAddress,
    UserType UserType
);

public record LoginRequest(string Email, string Password);

public record LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
}
