using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Ai;
using AiAlreadyDidIt.Api.Infrastructure.Auth;
using AiAlreadyDidIt.Api.Infrastructure.Email;
using AiAlreadyDidIt.Api.Infrastructure.Import;
using AiAlreadyDidIt.Api.Infrastructure.Jobs;
using AiAlreadyDidIt.Api.Infrastructure.Scanning;
using AiAlreadyDidIt.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ---------------------------------------------------------------- options
builder.Services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SiteOptions>(configuration.GetSection(SiteOptions.SectionName));
builder.Services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));
builder.Services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<ClamAvOptions>(configuration.GetSection(ClamAvOptions.SectionName));
builder.Services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
builder.Services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<RepositoryImportOptions>(configuration.GetSection(RepositoryImportOptions.SectionName));
builder.Services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<JobsOptions>(configuration.GetSection(JobsOptions.SectionName));

var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");

// ---------------------------------------------------------------- data
builder.Services.AddDbContext<AadiDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("AiAlreadyDidIt"),
            b => b.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName).UseVector())
        .UseSnakeCaseNamingConvention());

// ---------------------------------------------------------------- infrastructure
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<SiteSettingsCache>();
builder.Services.AddSingleton<ILlmProviderFactory, LlmProviderFactory>();
builder.Services.AddSingleton<IObjectStorage, S3ObjectStorage>();
builder.Services.AddSingleton<IVirusScanner, ClamAvScanner>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<JobQueue>();
builder.Services.AddScoped<IRepositoryImporter, GitHubRepositoryImporter>();
builder.Services.AddScoped<IRepositoryImporter, GitLabRepositoryImporter>();
builder.Services.AddScoped<ReferenceDataSeeder>();
builder.Services.AddScoped<DbSeeder>();
var emailProvider = configuration["Email:Provider"];
if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase)) builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
else builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();

// Every non-abstract class in the Services namespace whose name ends with "Service" is registered as scoped by
// convention (Gemecik pattern); job handlers ("*Job") are registered as IJobHandler.
foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
             .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } && t.Namespace is not null && t.Namespace.StartsWith("AiAlreadyDidIt.Api.Services", StringComparison.Ordinal)))
{
    if (type.Name.EndsWith("Service", StringComparison.Ordinal)) builder.Services.AddScoped(type);
    else if (type.Name.EndsWith("Job", StringComparison.Ordinal) && typeof(IJobHandler).IsAssignableFrom(type)) builder.Services.AddScoped(typeof(IJobHandler), type);
}
builder.Services.AddHostedService<BackgroundJobRunner>();
builder.Services.AddSingleton<ApiKeyUsageTracker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ApiKeyUsageTracker>());

foreach (var name in new[] { OllamaLlmProvider.HttpClientName, OpenAiCompatibleLlmProvider.HttpClientName, GitHubRepositoryImporter.HttpClientName, GitLabRepositoryImporter.HttpClientName, "download" })
    builder.Services.AddHttpClient(name, client => client.DefaultRequestHeaders.UserAgent.ParseAdd("AiAlreadyDidIt/1.0 (+https://github.com)"));

// ---------------------------------------------------------------- request limits (large installers)
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 600L * 1024 * 1024);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 600L * 1024 * 1024);
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// ---------------------------------------------------------------- CORS (SPA)
var corsOrigins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithExposedHeaders("Content-Disposition", "Retry-After");
    if (builder.Environment.IsDevelopment())
        policy.SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var u) && u.Host is "localhost" or "127.0.0.1");
}));

// ---------------------------------------------------------------- MVC (classic controllers)
builder.Services
    .AddControllers()
    .AddJsonOptions(options => AadiJson.Configure(options.JsonSerializerOptions))
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Where(kv => kv.Value?.Errors.Count > 0)
                .SelectMany(kv => kv.Value!.Errors.Select(e => ApiErrors.Unprocessable(string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage, string.IsNullOrEmpty(kv.Key) ? null : char.ToLowerInvariant(kv.Key[0]) + kv.Key[1..])))
                .ToList();
            return new ObjectResult(ApiResponse.Fail(ErrorCodes.UnprocessableEntity, MessageGroups.UnprocessableEntity, errors)) { StatusCode = StatusCodes.Status422UnprocessableEntity };
        };
    });

// ---------------------------------------------------------------- authentication: JWT (members) + API key (agents)
const string AutoScheme = "Auto";
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = AutoScheme;
        options.DefaultChallengeScheme = AutoScheme;
    })
    .AddPolicyScheme(AutoScheme, "JWT or API key", options =>
    {
        options.ForwardDefaultSelector = context => ApiKeyAuthenticationHandler.ExtractKey(context.Request) is not null ? ApiKeyAuthenticationHandler.SchemeName : JwtBearerDefaults.AuthenticationScheme;
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, _ => { })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = jwt.Issuer, ValidateAudience = true, ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (context.Response.HasStarted) return;
                await context.Response.WriteEnvelopeAsync(StatusCodes.Status401Unauthorized,
                    ApiResponse.Fail(ErrorCodes.Unauthorized, MessageGroups.Unauthorized, ApiErrors.Unauthorized("Sign in to continue (Bearer access token) or send an API key (X-Api-Key).")), context.HttpContext.RequestAborted);
            },
            OnForbidden = context => context.Response.WriteEnvelopeAsync(StatusCodes.Status403Forbidden,
                ApiResponse.Fail(ErrorCodes.Forbidden, MessageGroups.Forbidden, ApiErrors.Forbidden("You are not allowed to perform this action.")), context.HttpContext.RequestAborted)
        };
    });
builder.Services.AddAuthorization();

// ---------------------------------------------------------------- rate limiting (per API key / member / IP)
var rateLimiting = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
if (rateLimiting.Enabled)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        static string PartitionKey(HttpContext context)
        {
            var apiKey = context.User.FindFirst(CurrentUser.ApiKeyClaim)?.Value;
            if (apiKey is not null) return "key:" + apiKey;
            if (context.User.Identity?.IsAuthenticated == true) return "user:" + context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return "ip:" + (context.ClientIp() ?? "unknown");
        }
        int PermitFor(HttpContext context)
        {
            var apiKey = context.User.FindFirst(CurrentUser.ApiKeyClaim)?.Value;
            if (apiKey is not null)
                return context.User.FindFirst("rate_tier")?.Value switch { "unlimited" => int.MaxValue, "elevated" => rateLimiting.ApiKeyElevatedPermitLimit, _ => rateLimiting.ApiKeyDefaultPermitLimit };
            return context.User.Identity?.IsAuthenticated == true ? rateLimiting.UserPermitLimit : rateLimiting.AnonymousPermitLimit;
        }
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            if (!context.Request.Path.StartsWithSegments("/api")) return RateLimitPartition.GetNoLimiter("no-limit");
            var permit = PermitFor(context);
            if (permit == int.MaxValue) return RateLimitPartition.GetNoLimiter("unlimited");
            return RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context), _ => new FixedWindowRateLimiterOptions { PermitLimit = permit, Window = TimeSpan.FromSeconds(rateLimiting.WindowSeconds), QueueLimit = 0, AutoReplenishment = true });
        });
        options.AddPolicy("downloads", context => RateLimitPartition.GetFixedWindowLimiter("dl:" + PartitionKey(context), _ => new FixedWindowRateLimiterOptions { PermitLimit = rateLimiting.DownloadPermitLimit, Window = TimeSpan.FromSeconds(rateLimiting.WindowSeconds), QueueLimit = 0, AutoReplenishment = true }));
        options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter("auth:ip:" + (context.ClientIp() ?? "unknown"), _ => new FixedWindowRateLimiterOptions { PermitLimit = rateLimiting.AuthPermitLimit, Window = TimeSpan.FromSeconds(rateLimiting.WindowSeconds), QueueLimit = 0, AutoReplenishment = true }));
        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.Headers.RetryAfter = rateLimiting.WindowSeconds.ToString();
            await context.HttpContext.Response.WriteEnvelopeAsync(StatusCodes.Status429TooManyRequests, ApiResponse.Fail(ErrorCodes.TooManyRequests, MessageGroups.RateLimit, ApiErrors.RateLimit(rateLimiting.WindowSeconds)), ct);
        };
    });
}

// ---------------------------------------------------------------- OpenAPI + Scalar UI
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<SecuritySchemeTransformer>());

var app = builder.Build();

// ---------------------------------------------------------------- pipeline
app.UseForwardedHeaders();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("AI Already Did It API").WithTheme(ScalarTheme.Moon));
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<ApiKeyUsageMiddleware>();
if (rateLimiting.Enabled) app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/healthz", async (AadiDbContext db, CancellationToken ct) => { await db.Database.ExecuteSqlRawAsync("SELECT 1", ct); return Results.Ok(new { ok = true, time = Clock.Now }); }).AllowAnonymous();

// ---------------------------------------------------------------- database & storage bootstrap
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    if (dbOptions.MigrateOnStartup)
    {
        var db = scope.ServiceProvider.GetRequiredService<AadiDbContext>();
        await db.Database.MigrateAsync();
    }
    await scope.ServiceProvider.GetRequiredService<ReferenceDataSeeder>().EnsureAsync();
    try { await scope.ServiceProvider.GetRequiredService<IObjectStorage>().EnsureBucketsAsync(); }
    catch (Exception ex) { logger.LogError(ex, "Object storage is not reachable — uploads will fail until it is."); }
    if (dbOptions.SeedOnStartup)
    {
        try { await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync(); }
        catch (Exception ex) { logger.LogError(ex, "Demo seed failed"); }
    }
}

app.Run();

public partial class Program;
