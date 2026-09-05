using System.Linq.Expressions;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Entities;

namespace AiAlreadyDidIt.Api.Services.Catalog;

/// <summary>Single place that shapes an <see cref="App"/> row into the card DTO (server-side projection).</summary>
public static class AppCardProjection
{
    public static readonly Expression<Func<App, AppCardDto>> ToCard = a => new AppCardDto
    {
        Id = a.Id,
        Slug = a.Slug,
        Name = a.Name,
        ShortDescription = a.ShortDescription,
        IconUrl = a.IconStorageKey == null ? null : "/files/icons/" + a.IconStorageKey,
        CoverUrl = a.Screenshots.OrderBy(s => s.SortOrder).Select(s => "/files/screenshots/" + s.ThumbStorageKey).FirstOrDefault(),
        CategorySlug = a.Category.Slug,
        CategoryNameEn = a.Category.NameEn,
        CategoryNameTr = a.Category.NameTr,
        LicenseSpdxId = a.License.SpdxId,
        LlmModelName = a.LlmModel == null ? null : (a.LlmModel.Vendor + " " + a.LlmModel.Name + (a.LlmModel.Version == null ? "" : " " + a.LlmModel.Version)),
        LlmModelSlug = a.LlmModel == null ? null : a.LlmModel.Slug,
        Platforms = a.Versions.Where(v => v.Id == a.LatestVersionId).SelectMany(v => v.Files).Where(f => f.Platform != null).Select(f => f.Platform!.Code).Distinct().ToList(),
        RatingAvg = a.RatingAvg,
        RatingCount = a.RatingCount,
        DownloadCount = a.DownloadCount,
        LatestVersion = a.Versions.Where(v => v.Id == a.LatestVersionId).Select(v => v.Version).FirstOrDefault(),
        PublishedAt = a.PublishedAt,
        UpdatedAt = a.UpdatedAt,
        UploaderUsername = a.Uploader.Username,
        IsFeatured = a.IsFeatured,
        EstGenerationTokens = a.EstGenerationTokens,
        EstGenerationCostUsd = a.EstGenerationCostUsd,
        Status = a.Status,
        DerivedFromAppId = a.DerivedFromAppId
    };
}
