using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AiAlreadyDidIt.Tests;

public class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new() { Key = "unit-test-key-that-is-long-enough-for-hmac-sha256!!", Issuer = "aadi-test", Audience = "aadi-test-api", AccessTokenMinutes = 15 };
    private readonly JwtTokenService _svc = new(Microsoft.Extensions.Options.Options.Create(Options));

    private static User Admin() => new() { Id = 42, Username = "admin", Email = "admin@example.com", Role = new Role { Name = "Admin", IsAdmin = true } };

    [Fact]
    public void Access_token_carries_identity_claims_and_validates()
    {
        var (token, expires) = _svc.CreateAccessToken(Admin());
        Assert.InRange(expires, Clock.Now.AddMinutes(14), Clock.Now.AddMinutes(16));

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Key)),
            ClockSkew = TimeSpan.Zero
        }, out var validated);

        Assert.Equal("42", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("admin", principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("Admin", principal.FindFirstValue(ClaimTypes.Role));
        Assert.Equal("true", principal.FindFirstValue(CurrentUser.IsAdminClaim));
        Assert.Equal(SecurityAlgorithms.HmacSha256, ((JwtSecurityToken)validated).Header.Alg);
    }

    [Fact]
    public void Member_without_role_is_not_admin()
    {
        var (token, _) = _svc.CreateAccessToken(new User { Id = 7, Username = "demo", Email = "demo@example.com" });
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("false", jwt.Claims.First(c => c.Type == CurrentUser.IsAdminClaim).Value);
        Assert.Equal("Member", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void Token_signed_with_another_key_is_rejected()
    {
        var (token, _) = _svc.CreateAccessToken(Admin());
        var handler = new JwtSecurityTokenHandler();
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-different-key-that-is-also-long-enough-0123456789"))
        }, out _));
    }

    [Fact]
    public void Refresh_tokens_are_random_and_hashed_with_sha256()
    {
        var a = _svc.NewRefreshToken();
        var b = _svc.NewRefreshToken();
        Assert.NotEqual(a, b);
        Assert.Equal(64, a.Length);
        Assert.Equal(TextUtil.Sha256Hex(a), JwtTokenService.HashToken(a));
        Assert.Equal(64, JwtTokenService.HashToken(a).Length);
    }
}
