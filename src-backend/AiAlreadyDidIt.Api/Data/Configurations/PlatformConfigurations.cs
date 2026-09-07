using AiAlreadyDidIt.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAlreadyDidIt.Api.Data.Configurations;

public class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
{
    public void Configure(EntityTypeBuilder<ModerationAction> b)
    {
        b.Property(x => x.Note).HasMaxLength(2000);
        b.Property(x => x.Action).HasConversion<int>();
        b.HasIndex(x => x.AppId);
        b.HasOne(x => x.App).WithMany().HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ScanResultConfiguration : IEntityTypeConfiguration<ScanResult>
{
    public void Configure(EntityTypeBuilder<ScanResult> b)
    {
        b.Property(x => x.Engine).HasMaxLength(32).IsRequired();
        b.Property(x => x.Signature).HasMaxLength(256);
        b.Property(x => x.Raw).HasMaxLength(2000);
        b.Property(x => x.Verdict).HasConversion<int>();
        b.HasIndex(x => x.FileId);
    }
}

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> b)
    {
        b.Property(x => x.Key).HasMaxLength(128).IsRequired();
        b.Property(x => x.Value).HasMaxLength(8000);
        b.Property(x => x.Group).HasMaxLength(32).IsRequired();
        b.Property(x => x.ValueType).HasMaxLength(16).IsRequired();
        b.Property(x => x.Description).HasMaxLength(512);
        b.HasIndex(x => x.Key).IsUnique();
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(x => x.Username).HasMaxLength(64).IsRequired();
        b.Property(x => x.Action).HasMaxLength(128).IsRequired();
        b.Property(x => x.Entity).HasMaxLength(64);
        b.Property(x => x.EntityId).HasMaxLength(64);
        b.Property(x => x.Details).HasColumnType("text");
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => new { x.Entity, x.EntityId });
        b.HasIndex(x => x.UserId);
    }
}

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> b)
    {
        b.Property(x => x.Title).HasMaxLength(160).IsRequired();
        b.Property(x => x.Subtitle).HasMaxLength(300);
        b.Property(x => x.ImageStorageKey).HasMaxLength(400);
        b.Property(x => x.Link).HasMaxLength(512);
        b.Property(x => x.Position).HasMaxLength(16).IsRequired();
    }
}

public class BackgroundJobConfiguration : IEntityTypeConfiguration<BackgroundJob>
{
    public void Configure(EntityTypeBuilder<BackgroundJob> b)
    {
        b.Property(x => x.Type).HasMaxLength(64).IsRequired();
        b.Property(x => x.PayloadJson).HasColumnType("text").IsRequired();
        b.Property(x => x.LastError).HasMaxLength(4000);
        b.Property(x => x.Subject).HasMaxLength(64);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => new { x.Status, x.RunAt });
        b.HasIndex(x => x.Subject);
    }
}
