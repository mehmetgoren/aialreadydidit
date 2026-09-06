using AiAlreadyDidIt.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;

namespace AiAlreadyDidIt.Tests;

public class ApiKeyExtractionTests
{
    private static HttpRequest Request(string? apiKeyHeader = null, string? authorization = null)
    {
        var ctx = new DefaultHttpContext();
        if (apiKeyHeader is not null) ctx.Request.Headers[ApiKeyAuthenticationHandler.HeaderName] = apiKeyHeader;
        if (authorization is not null) ctx.Request.Headers.Authorization = authorization;
        return ctx.Request;
    }

    [Fact]
    public void Reads_the_x_api_key_header() => Assert.Equal("aad_abc", ApiKeyAuthenticationHandler.ExtractKey(Request(apiKeyHeader: "  aad_abc ")));

    [Fact]
    public void Reads_bearer_tokens_with_the_key_prefix() => Assert.Equal("aad_abc", ApiKeyAuthenticationHandler.ExtractKey(Request(authorization: "Bearer aad_abc")));

    [Fact]
    public void Leaves_jwt_bearer_tokens_alone() => Assert.Null(ApiKeyAuthenticationHandler.ExtractKey(Request(authorization: "Bearer eyJhbGciOi...")));

    [Fact]
    public void Header_wins_over_bearer() => Assert.Equal("aad_header", ApiKeyAuthenticationHandler.ExtractKey(Request("aad_header", "Bearer aad_bearer")));

    [Fact]
    public void Nothing_present_returns_null() => Assert.Null(ApiKeyAuthenticationHandler.ExtractKey(Request()));

    [Fact]
    public void Basic_authorization_is_ignored() => Assert.Null(ApiKeyAuthenticationHandler.ExtractKey(Request(authorization: "Basic dXNlcjpwYXNz")));
}
