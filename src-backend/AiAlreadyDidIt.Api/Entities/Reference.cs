namespace AiAlreadyDidIt.Api.Entities;

/// <summary>Hierarchical app category (up to three levels). Bilingual names.</summary>
public class Category
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    /// <summary>1 = root, 2 = subcategory, 3 = leaf.</summary>
    public int Level { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Element Plus icon name.</summary>
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Created by the LLM categoriser; waits for admin approval before it becomes browsable.</summary>
    public bool IsLlmProposed { get; set; }
    /// <summary>Cached count of published apps in this node (not descendants).</summary>
    public int AppCount { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<App> Apps { get; set; } = [];
}

/// <summary>Installable target. Each declared platform needs its own install file.</summary>
public class Platform
{
    public int Id { get; set; }
    /// <summary>windows | linux | macos | web | android | ios | docker | cli</summary>
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    /// <summary>Comma separated lower-case extensions accepted as installers (".exe,.msi").</summary>
    public string AllowedExtensions { get; set; } = string.Empty;
    /// <summary>Docker image references / hosted web bundles may be given as a reference instead of a file.</summary>
    public bool AllowsExternalReference { get; set; }
    public string? InstallHint { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>SPDX license. Only <see cref="IsAllowed"/> licenses satisfy golden rule 1.</summary>
public class License
{
    public int Id { get; set; }
    public string SpdxId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public bool IsOsiApproved { get; set; }
    public bool IsFsfLibre { get; set; }
    public bool IsAllowed { get; set; } = true;
    /// <summary>Family key used to compare detected vs declared licenses (e.g. GPL-3.0).</summary>
    public string? Family { get; set; }
    public int SortOrder { get; set; }
    public int AppCount { get; set; }
}

/// <summary>Generating LLM (vendor + model + version).</summary>
public class LlmModel
{
    public int Id { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateOnly? ReleasedOn { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public int AppCount { get; set; }
    public string DisplayName => string.IsNullOrEmpty(Version) ? $"{Vendor} {Name}" : $"{Vendor} {Name} {Version}";
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public bool IsBlocked { get; set; }
    public ICollection<AppTag> AppTags { get; set; } = [];
}
