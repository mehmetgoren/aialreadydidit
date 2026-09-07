using AiAlreadyDidIt.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAlreadyDidIt.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Username).HasMaxLength(40).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(256);
        b.Property(x => x.Bio).HasMaxLength(1000);
        b.Property(x => x.Locale).HasMaxLength(8).IsRequired();
        b.Property(x => x.AdminNote).HasMaxLength(2000);
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.Username).IsUnique();
        b.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.Property(x => x.Name).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> b)
    {
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.Route).HasMaxLength(256);
        b.Property(x => x.Icon).HasMaxLength(64);
        b.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoleMenuConfiguration : IEntityTypeConfiguration<RoleMenu>
{
    public void Configure(EntityTypeBuilder<RoleMenu> b)
    {
        b.HasIndex(x => new { x.RoleId, x.MenuId }).IsUnique();
        b.HasOne(x => x.Role).WithMany(r => r.RoleMenus).HasForeignKey(x => x.RoleId);
        b.HasOne(x => x.Menu).WithMany(m => m.RoleMenus).HasForeignKey(x => x.MenuId);
    }
}

public class RoleActionConfiguration : IEntityTypeConfiguration<RoleAction>
{
    public void Configure(EntityTypeBuilder<RoleAction> b)
    {
        b.Property(x => x.Controller).HasMaxLength(128).IsRequired();
        b.Property(x => x.Action).HasMaxLength(128).IsRequired();
        b.HasIndex(x => new { x.RoleId, x.Controller, x.Action }).IsUnique();
        b.HasOne(x => x.Role).WithMany(r => r.RoleActions).HasForeignKey(x => x.RoleId);
    }
}

public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> b)
    {
        b.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        b.Property(x => x.ProviderUserId).HasMaxLength(128).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256);
        b.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
        b.HasOne(x => x.User).WithMany(u => u.ExternalLogins).HasForeignKey(x => x.UserId);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
        b.Property(x => x.CreatedIp).HasMaxLength(64);
        b.Property(x => x.UserAgent).HasMaxLength(512);
        b.Ignore(x => x.IsActive);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId);
    }
}

public class EmailTokenConfiguration : IEntityTypeConfiguration<EmailToken>
{
    public void Configure(EntityTypeBuilder<EmailToken> b)
    {
        b.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.Kind).HasConversion<int>();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> b)
    {
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Property(x => x.Prefix).HasMaxLength(16).IsRequired();
        b.Property(x => x.KeyHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.Scopes).HasMaxLength(128).IsRequired();
        b.Property(x => x.RateTier).HasMaxLength(16).IsRequired();
        b.Ignore(x => x.IsActive);
        b.HasIndex(x => x.KeyHash).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasOne(x => x.User).WithMany(u => u.ApiKeys).HasForeignKey(x => x.UserId);
    }
}

public class ApiKeyUsageDailyConfiguration : IEntityTypeConfiguration<ApiKeyUsageDaily>
{
    public void Configure(EntityTypeBuilder<ApiKeyUsageDaily> b)
    {
        b.HasIndex(x => new { x.ApiKeyId, x.Date }).IsUnique();
    }
}
