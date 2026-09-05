using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AiAlreadyDidIt.Api.Infrastructure;

/// <summary>Adds the Bearer (JWT) and X-Api-Key security schemes to the OpenAPI document.</summary>
public sealed class SecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Title = "AI Already Did It API";
        document.Info.Description = "Search, download and publish LLM-generated applications. Agents: start with GET /api/v1/agent (usage guide) and GET /api/v1/agent/check?q=...";
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header, Description = "Access token from /api/v1/account/signin." };
        document.Components.SecuritySchemes["ApiKey"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, Name = "X-Api-Key", In = ParameterLocation.Header, Description = "Agent API key (aad_...), created in the member dashboard." };
        return Task.CompletedTask;
    }
}
