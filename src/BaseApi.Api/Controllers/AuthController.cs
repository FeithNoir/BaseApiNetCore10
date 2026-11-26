using System.Security.Claims;
using BaseApi.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BaseApi.Api.Controllers;

/// <summary>Controlador delgado: recibe, delega en IAuthService y elige el código HTTP.</summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await auth.RegisterAsync(req, ct));

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(TokenRequest req, CancellationToken ct)
    {
        await auth.VerifyEmailAsync(req, ct);
        return NoContent();
    }

    [HttpPost("login")]
    public Task<TokenPairResponse> Login(LoginRequest req, CancellationToken ct) => auth.LoginAsync(req, ct);

    [HttpPost("refresh")]
    public Task<TokenPairResponse> Refresh(RefreshRequest req, CancellationToken ct) => auth.RefreshAsync(req, ct);

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest req, CancellationToken ct)
    {
        await auth.LogoutAsync(req, ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req, CancellationToken ct)
    {
        await auth.ChangePasswordAsync(CurrentUserId(), req, ct);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(EmailRequest req, CancellationToken ct)
    {
        await auth.ForgotPasswordAsync(req, ct);
        return Accepted();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest req, CancellationToken ct)
    {
        await auth.ResetPasswordAsync(req, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public Task<UserResponse> Me(CancellationToken ct) => auth.MeAsync(CurrentUserId(), ct);

    // Con MapInboundClaims = false, "sub" llega tal cual (sin renombrarse a NameIdentifier).
    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
