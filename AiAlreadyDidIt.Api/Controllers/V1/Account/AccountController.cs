using AiAlreadyDidIt.Api.Contracts.Account;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Services.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Controllers.V1.Account;

/// <summary>Sign-up / sign-in / session management. The refresh token travels in an httpOnly cookie.</summary>
[ApiController]
[Route("api/v1/account")]
[Produces("application/json")]
public class AccountController(AccountService accounts, ICurrentUser currentUser, IOptions<JwtOptions> jwt, IHostEnvironment env) : ControllerBase
{
    private string CookieName => jwt.Value.RefreshCookieName;

    private void SetRefreshCookie(string token, DateTime expires) =>
        Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true, Secure = !env.IsDevelopment(), SameSite = SameSiteMode.Lax, Expires = expires, Path = "/api/v1/account", IsEssential = true
        });

    private void ClearRefreshCookie() => Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/api/v1/account" });

    /// <summary>Create an account with e-mail + password. Returns an access token and sets the refresh cookie.</summary>
    [HttpPost("signup")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> SignUp([FromBody] SignUpRequest request, CancellationToken ct)
    {
        var issued = await accounts.SignUpAsync(request, ct);
        SetRefreshCookie(issued.RefreshToken, issued.RefreshExpires);
        return Ok(ApiResponse.Ok(issued.Response));
    }

    /// <summary>Sign in with e-mail (or username) + password.</summary>
    [HttpPost("signin")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> SignIn([FromBody] SignInRequest request, CancellationToken ct)
    {
        var issued = await accounts.SignInAsync(request, ct);
        SetRefreshCookie(issued.RefreshToken, issued.RefreshExpires);
        return Ok(ApiResponse.Ok(issued.Response));
    }

    /// <summary>Sign in / sign up with a Google ID token (Google Identity Services on the client).</summary>
    [HttpPost("google")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Google([FromBody] GoogleSignInRequest request, CancellationToken ct)
    {
        var issued = await accounts.GoogleSignInAsync(request, ct);
        SetRefreshCookie(issued.RefreshToken, issued.RefreshExpires);
        return Ok(ApiResponse.Ok(issued.Response));
    }

    /// <summary>Rotate the refresh cookie and get a new access token.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(CancellationToken ct)
    {
        var issued = await accounts.RefreshAsync(Request.Cookies[CookieName], ct);
        SetRefreshCookie(issued.RefreshToken, issued.RefreshExpires);
        return Ok(ApiResponse.Ok(issued.Response));
    }

    /// <summary>Revoke the current session.</summary>
    [HttpPost("signout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<OkDto>>> SignOut(CancellationToken ct)
    {
        await accounts.SignOutAsync(Request.Cookies[CookieName], ct);
        ClearRefreshCookie();
        return Ok(ApiResponse.Ok(new OkDto()));
    }

    /// <summary>Signed-in member summary (counters for the header).</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MeDto>>> Me(CancellationToken ct) =>
        Ok(ApiResponse.Ok(await accounts.GetMeAsync(currentUser.Id, ct)));

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MeDto>>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await accounts.UpdateProfileAsync(request, ct)));

    [HttpPost("password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OkDto>>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await accounts.ChangePasswordAsync(request, ct);
        return Ok(ApiResponse.Ok(new OkDto()));
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<OkDto>>> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        await accounts.VerifyEmailAsync(request, ct);
        return Ok(ApiResponse.Ok(new OkDto()));
    }

    [HttpPost("resend-verification")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<OkDto>>> ResendVerification(CancellationToken ct)
    {
        await accounts.ResendVerificationAsync(ct);
        return Ok(ApiResponse.Ok(new OkDto()));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<OkDto>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await accounts.ForgotPasswordAsync(request, ct);
        return Ok(ApiResponse.Ok(new OkDto()));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<OkDto>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await accounts.ResetPasswordAsync(request, ct);
        return Ok(ApiResponse.Ok(new OkDto()));
    }

    /// <summary>Active sessions (refresh tokens) of the signed-in member.</summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<SessionDto>>>> Sessions(CancellationToken ct) =>
        Ok(ApiResponse.Ok(await accounts.GetSessionsAsync(Request.Cookies[CookieName], ct)));

    [HttpDelete("sessions/{id:long}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OkDto>>> RevokeSession(long id, CancellationToken ct)
    {
        await accounts.RevokeSessionAsync(id, ct);
        return Ok(ApiResponse.Ok(new OkDto()));
    }

    [HttpDelete("sessions")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OkDto>>> RevokeAllSessions(CancellationToken ct)
    {
        await accounts.RevokeAllSessionsAsync(ct);
        ClearRefreshCookie();
        return Ok(ApiResponse.Ok(new OkDto()));
    }
}
