using System.ComponentModel.DataAnnotations;

namespace AiAlreadyDidIt.Api.Contracts.Admin;

public class AdminCategoryNodeDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int Level { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsLlmProposed { get; set; }
    public int AppCount { get; set; }
    public int TotalAppCount { get; set; }
    public List<AdminCategoryNodeDto> Children { get; set; } = [];
}

public class SaveCategoryRequest
{
    [Required, MaxLength(120)] public string NameEn { get; set; } = string.Empty;
    [MaxLength(120)] public string? NameTr { get; set; }
    [MaxLength(120)] public string? Slug { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(64)] public string? Icon { get; set; }
    public int? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MoveCategoryRequest { public int? NewParentId { get; set; } }
public class ReorderCategoriesRequest { public List<int> OrderedIds { get; set; } = []; }
public class MergeRequest { public int TargetId { get; set; } }

public class AdminTagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public bool IsBlocked { get; set; }
}

public class SaveTagRequest
{
    [Required, MaxLength(48)] public string Name { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
}

public class AdminLicenseDto
{
    public int Id { get; set; }
    public string SpdxId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Family { get; set; }
    public bool IsOsiApproved { get; set; }
    public bool IsFsfLibre { get; set; }
    public bool IsAllowed { get; set; }
    public int SortOrder { get; set; }
    public int AppCount { get; set; }
}

public class SaveLicenseRequest
{
    [Required, MaxLength(64)] public string SpdxId { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(512)] public string? Url { get; set; }
    [MaxLength(64)] public string? Family { get; set; }
    public bool IsOsiApproved { get; set; }
    public bool IsFsfLibre { get; set; }
    public bool IsAllowed { get; set; } = true;
    public int SortOrder { get; set; }
}

public class AdminPlatformDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string AllowedExtensions { get; set; } = string.Empty;
    public bool AllowsExternalReference { get; set; }
    public string? InstallHint { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int FileCount { get; set; }
}

public class SavePlatformRequest
{
    [Required, MaxLength(32)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string Name { get; set; } = string.Empty;
    [MaxLength(64)] public string? Icon { get; set; }
    [Required, MaxLength(512)] public string AllowedExtensions { get; set; } = string.Empty;
    public bool AllowsExternalReference { get; set; }
    [MaxLength(1000)] public string? InstallHint { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AdminLlmModelDto
{
    public int Id { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateOnly? ReleasedOn { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public int AppCount { get; set; }
}

public class SaveLlmModelRequest
{
    [Required, MaxLength(64)] public string Vendor { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string Name { get; set; } = string.Empty;
    [MaxLength(64)] public string? Version { get; set; }
    public DateOnly? ReleasedOn { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
