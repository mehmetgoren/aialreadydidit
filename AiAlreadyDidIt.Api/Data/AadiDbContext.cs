using AiAlreadyDidIt.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Data;

public class AadiDbContext(DbContextOptions<AadiDbContext> options) : DbContext(options)
{
    // identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();
    public DbSet<RoleAction> RoleActions => Set<RoleAction>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailToken> EmailTokens => Set<EmailToken>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ApiKeyUsageDaily> ApiKeyUsageDaily => Set<ApiKeyUsageDaily>();

    // reference lists
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Platform> Platforms => Set<Platform>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<LlmModel> LlmModels => Set<LlmModel>();
    public DbSet<Tag> Tags => Set<Tag>();

    // catalogue
    public DbSet<App> Apps => Set<App>();
    public DbSet<AppVersion> AppVersions => Set<AppVersion>();
    public DbSet<AppFile> AppFiles => Set<AppFile>();
    public DbSet<AppScreenshot> AppScreenshots => Set<AppScreenshot>();
    public DbSet<AppPrompt> AppPrompts => Set<AppPrompt>();
    public DbSet<AppTag> AppTags => Set<AppTag>();

    // engagement
    public DbSet<Download> Downloads => Set<Download>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<RatingReply> RatingReplies => Set<RatingReply>();
    public DbSet<RatingVote> RatingVotes => Set<RatingVote>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionItem> CollectionItems => Set<CollectionItem>();
    public DbSet<AppWatch> AppWatches => Set<AppWatch>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<SearchLog> SearchLogs => Set<SearchLog>();
    public DbSet<AppRequest> AppRequests => Set<AppRequest>();
    public DbSet<AppRequestVote> AppRequestVotes => Set<AppRequestVote>();

    // moderation & platform
    public DbSet<ModerationAction> ModerationActions => Set<ModerationAction>();
    public DbSet<ScanResult> ScanResults => Set<ScanResult>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Everything is stored as UTC.
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp with time zone");
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
        configurationBuilder.Properties<string>().HaveMaxLength(512);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AadiDbContext).Assembly);
    }
}
