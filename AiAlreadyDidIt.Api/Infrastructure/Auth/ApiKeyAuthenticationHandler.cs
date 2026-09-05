using System.Security.Claims;
using System.Text.Encodings.Web;
using AiAlreadyDidIt.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Infrastructure.Auth;

/// <summary>
/// Authenticates agent requests carrying <c>X-Api-Key: aad_…</c> (or <c>Authorization: Bearer aad_…</c>).
/// The principal carries the key owner's user id plus the key id and scopes.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, AadiDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string KeyPrefix = "aad_";

    public static string? ExtractKey(HttpRequest request)
    {
        var header = request.Headers[HeaderName].ToString();
        if (!string.IsNullOrWhiteSpace(header)) return header.Trim();
        var auth = request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var value = auth[7..].Trim();
            if (value.StartsWith(KeyPrefix, StringComparison.Ordinal)) return value;
        }
        return null;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var raw = ExtractKey(Request);
        if (raw is null) return AuthenticateResult.NoResult();
        if (!raw.StartsWith(KeyPrefix, StringComparison.Ordinal)) return AuthenticateResult.Fail("Invalid API key format.");

        var hash = TextUtil.Sha256Hex(raw);
        var key = await db.ApiKeys.AsNoTracking().Include(k => k.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(k => k.KeyHash == hash, Context.RequestAborted);
        if (key is null || key.RevokedAt is not null || (key.ExpiresAt is not null && key.ExpiresAt <= AiAlreadyDidIt.Api.Infrastructure.Clock.Now))
            return AuthenticateResult.Fail("API key is invalid, revoked or expired.");
        if (!key.User.IsActive || key.User.IsBanned)
            return AuthenticateResult.Fail("The account owning this API key is disabled.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, key.UserId.ToString()),
            new(ClaimTypes.Name, key.User.Username),
            new(ClaimTypes.Role, key.User.Role.Name),
            new(CurrentUser.IsAdminClaim, "false"), // keys never grant admin rights
            new(CurrentUser.ApiKeyClaim, key.Id.ToString()),
            new(CurrentUser.ScopesClaim, key.Scopes),
            new("rate_tier", key.RateTier)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        Context.Items["ApiKeyId"] = key.Id;
        return AuthenticateResult.Success(ticket);
    }
}
