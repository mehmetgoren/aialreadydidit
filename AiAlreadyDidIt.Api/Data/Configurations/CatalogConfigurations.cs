using AiAlreadyDidIt.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAlreadyDidIt.Api.Data.Configurations;

public class AppConfiguration : IEntityTypeConfiguration<App>
{
    public const int EmbeddingDimensions = 1024;

    public void Configure(EntityTypeBuilder<App> b)
    {
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.ShortDescription).HasMaxLength(200).IsRequired();
        b.Property(x => x.LongDescription).HasColumnType("text").IsRequired();
        b.Property(x => x.LlmModelNote).HasMaxLength(200);
        b.Property(x => x.RepoUrl).HasMaxLength(512);
        b.Property(x => x.RepoOwner).HasMaxLength(120);
        b.Property(x => x.RepoName).HasMaxLength(120);
        b.Property(x => x.RepoDefaultBranch).HasMaxLength(120);
        b.Property(x => x.RepoPrimaryLanguage).HasMaxLength(64);
        b.Property(x => x.ReadmeMarkdown).HasColumnType("text");
        b.Property(x => x.IconStorageKey).HasMaxLength(256);
        b.Property(x => x.HomepageUrl).HasMaxLength(512);
        b.Property(x => x.RejectionReason).HasMaxLength(2000);
        b.Property(x => x.TagsText).HasMaxLength(1000).IsRequired();
        b.Property(x => x.CategoryPathText).HasMaxLength(400).IsRequired();
        b.Property(x => x.EmbeddingModel).HasMaxLength(120);
        b.Property(x => x.FeaturedNote).HasMaxLength(200);
        b.Property(x => x.DetectedLicenseSpdxId).HasMaxLength(64);
        b.Property(x => x.SourceWarnings).HasMaxLength(2000);
        b.Property(x => x.SourcePrimaryLanguage).HasMaxLength(64);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.SourceKind).HasConversion<int>();
        b.Property(x => x.RepoProvider).HasConversion<int>();
        b.Property(x => x.DerivationKind).HasConversion<int>();

        // Full-text: weighted generated column (name A, short description B, tags/category C, long description D).
        b.Property(x => x.SearchVector)
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                "setweight(to_tsvector('english', coalesce(name, '')) || to_tsvector('simple', coalesce(name, '')), 'A') || " +
                "setweight(to_tsvector('english', coalesce(short_description, '')) || to_tsvector('simple', coalesce(short_description, '')), 'B') || " +
                "setweight(to_tsvector('english', coalesce(tags_text, '') || ' ' || coalesce(category_path_text, '')) || to_tsvector('simple', coalesce(tags_text, '') || ' ' || coalesce(category_path_text, '')), 'C') || " +
                "setweight(to_tsvector('english', left(coalesce(long_description, ''), 20000)) || to_tsvector('simple', left(coalesce(long_description, ''), 20000)), 'D')",
                stored: true);
        b.HasIndex(x => x.SearchVector).HasMethod("GIN");
        b.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops").HasDatabaseName("ix_apps_name_trgm");

        // Semantic: cosine HNSW.
        b.Property(x => x.Embedding).HasColumnType($"vector({EmbeddingDimensions})");
        b.HasIndex(x => x.Embedding).HasMethod("hnsw").HasOperators("vector_cosine_ops").HasDatabaseName("ix_apps_embedding_hnsw");

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.Status, x.CategoryId });
        b.HasIndex(x => new { x.Status, x.PublishedAt });
        b.HasIndex(x => x.UploaderUserId);
        b.HasIndex(x => x.DerivedFromAppId);

        b.HasOne(x => x.Category).WithMany(c => c.Apps).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.License).WithMany().HasForeignKey(x => x.LicenseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Uploader).WithMany(u => u.Apps).HasForeignKey(x => x.UploaderUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.LlmModel).WithMany().HasForeignKey(x => x.LlmModelId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.DerivedFrom).WithMany(a => a.Derivatives).HasForeignKey(x => x.DerivedFromAppId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class AppVersionConfiguration : IEntityTypeConfiguration<AppVersion>
{
    public void Configure(EntityTypeBuilder<AppVersion> b)
    {
        b.Property(x => x.Version).HasMaxLength(64).IsRequired();
        b.Property(x => x.Changelog).HasColumnType("text");
        b.Property(x => x.SourceRef).HasMaxLength(200);
        b.Property(x => x.RejectionReason).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => new { x.AppId, x.Version }).IsUnique();
        b.HasOne(x => x.App).WithMany(a => a.Versions).HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AppFileConfiguration : IEntityTypeConfiguration<AppFile>
{
    public void Configure(EntityTypeBuilder<AppFile> b)
    {
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.StorageKey).HasMaxLength(400);
        b.Property(x => x.ExternalReference).HasMaxLength(512);
        b.Property(x => x.Sha256).HasMaxLength(64);
        b.Property(x => x.ContentType).HasMaxLength(128);
        b.Property(x => x.ScanSignature).HasMaxLength(256);
        b.Property(x => x.InstallHint).HasMaxLength(1000);
        b.Property(x => x.Kind).HasConversion<int>();
        b.Property(x => x.ScanStatus).HasConversion<int>();
        b.HasIndex(x => x.VersionId);
        b.HasIndex(x => x.ScanStatus);
        b.HasOne(x => x.Version).WithMany(v => v.Files).HasForeignKey(x => x.VersionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Platform).WithMany().HasForeignKey(x => x.PlatformId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AppScreenshotConfiguration : IEntityTypeConfiguration<AppScreenshot>
{
    public void Configure(EntityTypeBuilder<AppScreenshot> b)
    {
        b.Property(x => x.StorageKey).HasMaxLength(400).IsRequired();
        b.Property(x => x.ThumbStorageKey).HasMaxLength(400).IsRequired();
        b.Property(x => x.Caption).HasMaxLength(200);
        b.HasIndex(x => x.AppId);
        b.HasOne(x => x.App).WithMany(a => a.Screenshots).HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AppPromptConfiguration : IEntityTypeConfiguration<AppPrompt>
{
    public void Configure(EntityTypeBuilder<AppPrompt> b)
    {
        b.Property(x => x.Title).HasMaxLength(160).IsRequired();
        b.Property(x => x.PromptText).HasColumnType("text").IsRequired();
        b.HasIndex(x => x.AppId);
        b.HasOne(x => x.App).WithMany(a => a.Prompts).HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AppTagConfiguration : IEntityTypeConfiguration<AppTag>
{
    public void Configure(EntityTypeBuilder<AppTag> b)
    {
        b.HasKey(x => new { x.AppId, x.TagId });
        b.HasOne(x => x.App).WithMany(a => a.AppTags).HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Tag).WithMany(t => t.AppTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.TagId);
    }
}
