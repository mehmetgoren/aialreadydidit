using AiAlreadyDidIt.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAlreadyDidIt.Api.Data.Configurations;

public class DownloadConfiguration : IEntityTypeConfiguration<Download>
{
    public void Configure(EntityTypeBuilder<Download> b)
    {
        b.Property(x => x.IpHash).HasMaxLength(32);
        b.Property(x => x.UserAgent).HasMaxLength(512);
        b.Property(x => x.Source).HasConversion<int>();
        b.HasIndex(x => new { x.AppId, x.CreatedAt });
        b.HasIndex(x => new { x.UserId, x.AppId });
        b.HasIndex(x => x.CreatedAt);
    }
}

public class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> b)
    {
        b.Property(x => x.Review).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => new { x.AppId, x.UserId }).IsUnique();
        b.HasOne(x => x.App).WithMany(a => a.Ratings).HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany(u => u.Ratings).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Version).WithMany().HasForeignKey(x => x.VersionId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class RatingReplyConfiguration : IEntityTypeConfiguration<RatingReply>
{
    public void Configure(EntityTypeBuilder<RatingReply> b)
    {
        b.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        b.HasOne(x => x.Rating).WithMany(r => r.Replies).HasForeignKey(x => x.RatingId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RatingVoteConfiguration : IEntityTypeConfiguration<RatingVote>
{
    public void Configure(EntityTypeBuilder<RatingVote> b) => b.HasKey(x => new { x.RatingId, x.UserId });
}

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> b)
    {
        b.HasKey(x => new { x.UserId, x.AppId });
        b.HasOne(x => x.App).WithMany().HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> b)
    {
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => new { x.UserId, x.Slug }).IsUnique();
        b.HasOne(x => x.User).WithMany(u => u.Collections).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CollectionItemConfiguration : IEntityTypeConfiguration<CollectionItem>
{
    public void Configure(EntityTypeBuilder<CollectionItem> b)
    {
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasIndex(x => new { x.CollectionId, x.AppId }).IsUnique();
        b.HasOne(x => x.Collection).WithMany(c => c.Items).HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.App).WithMany().HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AppWatchConfiguration : IEntityTypeConfiguration<AppWatch>
{
    public void Configure(EntityTypeBuilder<AppWatch> b)
    {
        b.HasIndex(x => new { x.UserId, x.AppId }).IsUnique();
        b.HasOne(x => x.App).WithMany().HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Body).HasMaxLength(2000);
        b.Property(x => x.Link).HasMaxLength(512);
        b.Property(x => x.Type).HasConversion<int>();
        b.HasIndex(x => new { x.UserId, x.ReadAt });
        b.HasOne(x => x.User).WithMany(u => u.Notifications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.Property(x => x.Details).HasMaxLength(4000);
        b.Property(x => x.Resolution).HasMaxLength(2000);
        b.Property(x => x.ReporterEmail).HasMaxLength(256);
        b.Property(x => x.IpHash).HasMaxLength(32);
        b.Property(x => x.Reason).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.AppId);
        b.HasOne(x => x.App).WithMany().HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SearchLogConfiguration : IEntityTypeConfiguration<SearchLog>
{
    public void Configure(EntityTypeBuilder<SearchLog> b)
    {
        b.Property(x => x.Query).HasMaxLength(500).IsRequired();
        b.Property(x => x.FiltersJson).HasMaxLength(2000);
        b.Property(x => x.Mode).HasConversion<int>();
        b.Property(x => x.Source).HasConversion<int>();
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.ResultCount);
    }
}

public class AppRequestConfiguration : IEntityTypeConfiguration<AppRequest>
{
    public void Configure(EntityTypeBuilder<AppRequest> b)
    {
        b.Property(x => x.Title).HasMaxLength(160).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Embedding).HasColumnType($"vector({AppConfiguration.EmbeddingDimensions})");
        b.Property(x => x.Source).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => x.Status);
        b.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.FulfilledByApp).WithMany().HasForeignKey(x => x.FulfilledByAppId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class AppRequestVoteConfiguration : IEntityTypeConfiguration<AppRequestVote>
{
    public void Configure(EntityTypeBuilder<AppRequestVote> b) => b.HasKey(x => new { x.RequestId, x.UserId });
}
