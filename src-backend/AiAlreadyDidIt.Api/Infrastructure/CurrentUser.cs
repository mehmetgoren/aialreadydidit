using System.Security.Claims;
using AiAlreadyDidIt.Api.Contracts.Common;

namespace AiAlreadyDidIt.Api.Infrastructure;

public interface ICurrentUser
{
    int Id { get; }
    int? IdOrNull { get; }
    string Username { get; }
    bool IsAuthenticated { get; }
    string? RoleName { get; }
    bool IsAdmin { get; }
    /// <summary>Set when the request was authenticated with an agent API key.</summary>
    int? ApiKeyId { get; }
    bool IsApiKey { get; }
    string[] Scopes { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsWebClient { get; }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string ApiKeyClaim = "api_key_id";
    public const string ScopesClaim = "scopes";
    public const string IsAdminClaim = "is_admin";

    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int Id => IdOrNull ?? throw ApiException.Unauthorized("Sign in to continue.");

    public int? IdOrNull
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    public string Username => Principal?.FindFirstValue(ClaimTypes.Name) ?? Principal?.FindFirstValue("unique_name") ?? string.Empty;
    public string? RoleName => Principal?.FindFirstValue(ClaimTypes.Role) ?? Principal?.FindFirstValue("role");
    public bool IsAdmin => string.Equals(Principal?.FindFirstValue(IsAdminClaim), "true", StringComparison.OrdinalIgnoreCase);

    public int? ApiKeyId => int.TryParse(Principal?.FindFirstValue(ApiKeyClaim), out var id) ? id : null;
    public bool IsApiKey => ApiKeyId is not null;
    public string[] Scopes => (Principal?.FindFirstValue(ScopesClaim) ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public string? IpAddress => accessor.HttpContext?.ClientIp();
    public string? UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString();
    public bool IsWebClient => accessor.HttpContext?.Request.Headers.ContainsKey("X-Aadi-Client") == true;
}

public static class HttpContextExtensions
{
    /// <summary>Client IP honouring X-Forwarded-For (nginx in front).</summary>
    public static string? ClientIp(this HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return Normalize(forwarded.Split(',')[0].Trim());
        return context.Connection.RemoteIpAddress is { } ip ? Normalize(ip.ToString()) : null;
    }

    /// <summary>"::ffff:1.2.3.4" (IPv4 mapped into IPv6 by the dual-stack listener) → "1.2.3.4".</summary>
    private static string Normalize(string value) =>
        System.Net.IPAddress.TryParse(value, out var ip) && ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4().ToString() : value;
}
