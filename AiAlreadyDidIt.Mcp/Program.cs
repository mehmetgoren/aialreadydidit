using AiAlreadyDidIt.Mcp;

// `--stdio` runs the server over stdin/stdout for local MCP clients (Claude Code, Claude Desktop, Cursor...).
// Without it the server listens on HTTP (Streamable HTTP transport) at /mcp — the mode used by docker-compose.
if (args.Contains("--stdio"))
{
    var host = Host.CreateApplicationBuilder(args);
    host.Logging.ClearProviders();
    host.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
    host.Services.AddHttpClient(AadiApiClient.HttpClientName);
    host.Services.AddSingleton<AadiApiClient>();
    host.Services.AddMcpServer(o => { o.ServerInfo = new() { Name = "ai-already-did-it", Version = "1.0.0" }; })
        .WithStdioServerTransport()
        .WithToolsFromAssembly()
        .WithPromptsFromAssembly();
    await host.Build().RunAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(AadiApiClient.HttpClientName);
builder.Services.AddScoped<AadiApiClient>();
builder.Services.AddMcpServer(o => { o.ServerInfo = new() { Name = "ai-already-did-it", Version = "1.0.0" }; })
    .WithHttpTransport(o => o.Stateless = true)
    .WithToolsFromAssembly()
    .WithPromptsFromAssembly();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Mcp-Session-Id")));

var app = builder.Build();
app.UseCors();
app.MapMcp("/mcp");
app.MapGet("/", (IConfiguration cfg) => Results.Json(new
{
    name = "AI Already Did It — MCP server",
    transport = "streamable-http",
    endpoint = "/mcp",
    tools = new[] { "check_before_building", "search_apps", "get_app", "list_categories", "download_app", "submit_app_request", "get_savings" },
    prompts = new[] { "reuse_first" },
    api = cfg["Api:PublicUrl"] ?? cfg["Api:BaseUrl"],
    auth = "optional: send X-Api-Key: aad_... (created in the member dashboard) to raise rate limits and enable submit_app_request",
    stdio = "run with --stdio for local clients"
}));
app.MapGet("/healthz", async (AadiApiClient api, CancellationToken ct) =>
{
    try { await api.GetAsync("api/v1/site/savings", ct); return Results.Ok(new { ok = true }); }
    catch (Exception ex) { return Results.Json(new { ok = false, error = ex.Message }, statusCode: 503); }
});
app.Run();
