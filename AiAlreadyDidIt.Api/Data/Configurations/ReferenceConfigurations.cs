using AiAlreadyDidIt.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAlreadyDidIt.Api.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(120).IsRequired();
        b.Property(x => x.NameTr).HasMaxLength(120).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Icon).HasMaxLength(64);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.ParentId);
        b.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PlatformConfiguration : IEntityTypeConfiguration<Platform>
{
    public void Configure(EntityTypeBuilder<Platform> b)
    {
        b.Property(x => x.Code).HasMaxLength(32).IsRequired();
        b.Property(x => x.Name).HasMaxLength(64).IsRequired();
        b.Property(x => x.Icon).HasMaxLength(64);
        b.Property(x => x.AllowedExtensions).HasMaxLength(512).IsRequired();
        b.Property(x => x.InstallHint).HasMaxLength(1000);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> b)
    {
        b.Property(x => x.SpdxId).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.Family).HasMaxLength(64);
        b.HasIndex(x => x.SpdxId).IsUnique();
    }
}

public class LlmModelConfiguration : IEntityTypeConfiguration<LlmModel>
{
    public void Configure(EntityTypeBuilder<LlmModel> b)
    {
        b.Property(x => x.Vendor).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(64).IsRequired();
        b.Property(x => x.Version).HasMaxLength(64);
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.Ignore(x => x.DisplayName);
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.Property(x => x.Name).HasMaxLength(48).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(48).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}
