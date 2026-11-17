using AssetFlow.Api.Contracts;
using AssetFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>Create a Staff account and get a token.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request) =>
        Ok(await auth.RegisterAsync(request));

    /// <summary>Log in with email and password to get a token.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await auth.LoginAsync(request);
        return result is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Wrong email or password")
            : Ok(result);
    }
}
