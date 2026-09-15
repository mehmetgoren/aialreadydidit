using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Import;
using AiAlreadyDidIt.Api.Infrastructure.Jobs;
using AiAlreadyDidIt.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Apps;

public sealed partial class AppEditorService
{
    // ---------------------------------------------------------------- repository import

    public async Task<RepositoryInspectionDto> InspectRepositoryAsync(InspectRepositoryRequest request, CancellationToken ct)
    {
        var url = RepositoryUrl.Normalize(request.RepoUrl) ?? throw ApiException.Unprocessable("Enter a valid GitHub or GitLab repository URL.", "repoUrl");
        var importer = importers.FirstOrDefault(i => i.CanHandle(url)) ?? throw ApiException.Unprocessable("Only GitHub and GitLab repositories can be imported. Upload a source archive instead.", "repoUrl");
        RepositoryInspection repo;
        try { repo = await importer.InspectAsync(url, ct); }
        catch (RepositoryImportException ex) { throw ApiException.Unprocessable(ex.Message, "repoUrl"); }
        catch (HttpRequestException ex) { throw ApiException.Unavailable("The repository host could not be reached: " + ex.Message); }
        return await MapInspectionAsync(repo, ct);
    }

    private async Task<RepositoryInspectionDto> MapInspectionAsync(RepositoryInspection repo, CancellationToken ct)
    {
        var detected = LicenseDetector.Detect(repo.LicenseText) ?? repo.LicenseSpdxId;
        var licenseId = detected is null ? null : await db.Licenses.Where(l => l.SpdxId == detected).Select(l => (int?)l.Id).FirstOrDefaultAsync(ct);
        var platforms = await db.Platforms.AsNoTracking().Where(p => p.IsActive).ToListAsync(ct);
        var dto = new RepositoryInspectionDto
        {
            Provider = repo.Provider, Owner = repo.Owner, Name = repo.Name, Url = repo.Url, Description = repo.Description, Homepage = repo.Homepage, DefaultBranch = repo.DefaultBranch,
            Stars = repo.Stars, PrimaryLanguage = repo.PrimaryLanguage, LicenseSpdxId = repo.LicenseSpdxId, DetectedLicenseSpdxId = detected, LicenseId = licenseId,
            HasReadme = !string.IsNullOrWhiteSpace(repo.ReadmeMarkdown), ReadmeExcerpt = TextUtil.Excerpt(repo.ReadmeMarkdown, 400), Topics = repo.Topics, Tags = repo.Tags, IsArchived = repo.IsArchived,
            Releases = repo.Releases.Select(r => new ReleaseDto
            {
                Tag = r.Tag, Name = r.Name, Body = r.Body, PublishedAt = r.PublishedAt,
                Assets = r.Assets.Select(a => new ReleaseAssetDto { Name = a.Name, Url = a.Url, Size = a.Size, SuggestedPlatform = GuessPlatform(a.Name, platforms) }).ToList()
            }).ToList()
        };
        if (repo.IsPrivate) dto.Warnings.Add("The repository is private; it must be public so other LLMs can read the source.");
        if (repo.IsArchived) dto.Warnings.Add("The repository is archived.");
        if (!dto.HasReadme) dto.Warnings.Add("No README found — add one, it becomes the long description.");
        if (detected is null) dto.Warnings.Add("No recognised LICENSE file — add one before submitting.");
        else if (licenseId is null) dto.Warnings.Add($"License {detected} is not in the accepted list.");
        return dto;
    }

    private static string? GuessPlatform(string fileName, List<Platform> platforms)
    {
        var lower = fileName.ToLowerInvariant();
        foreach (var p in platforms.Where(p => p.Code != "cli"))
            if (p.AllowedExtensions.Split(',').Any(ext => lower.EndsWith(ext.Trim()) && ext.Trim() is not (".zip" or ".tar.gz" or ".tgz" or ".tar.xz")))
                return p.Code;
        if (lower.Contains("win")) return "windows";
        if (lower.Contains("linux")) return "linux";
        if (lower.Contains("mac") || lower.Contains("darwin")) return "macos";
        return null;
    }

    public async Task<AppDraftDto> AttachRepositoryAsync(int id, AttachRepositoryRequest request, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var version = app.Versions.OrderByDescending(v => v.Id).FirstOrDefault(v => v.Status is VersionStatus.Draft or VersionStatus.Rejected) ?? throw ApiException.Unprocessable("No draft version.");
        var url = RepositoryUrl.Normalize(request.RepoUrl) ?? throw ApiException.Unprocessable("Enter a valid GitHub or GitLab repository URL.", "repoUrl");
        var importer = importers.FirstOrDefault(i => i.CanHandle(url)) ?? throw ApiException.Unprocessable("Only GitHub and GitLab repositories can be imported.", "repoUrl");
        RepositoryInspection repo;
        try { repo = await importer.InspectAsync(url, ct); }
        catch (RepositoryImportException ex) { throw ApiException.Unprocessable(ex.Message, "repoUrl"); }
        if (repo.IsPrivate) throw ApiException.Unprocessable("The repository must be public.", "repoUrl");

        // rule: source code OR repository, never both — drop any uploaded archive
        await RemoveSourceFilesAsync(version, ct);
        app.SourceKind = SourceKind.Repository;
        app.RepoUrl = repo.Url;
        app.RepoProvider = repo.Provider == "GitLab" ? RepoProvider.GitLab : RepoProvider.GitHub;
        app.RepoOwner = repo.Owner;
        app.RepoName = repo.Name;
        app.RepoDefaultBranch = repo.DefaultBranch;
        app.RepoStars = repo.Stars;
        app.RepoPrimaryLanguage = repo.PrimaryLanguage;
        app.RepoSyncedAt = Clock.Now;
        app.SourceAnalyzedAt = null;
        app.SourceWarnings = "Importing the repository snapshot…";
        version.SourceRef = string.IsNullOrWhiteSpace(request.SourceRef) ? null : request.SourceRef.Trim();

        if (request.Prefill)
        {
            if ((app.Name == "Untitled app" || app.Name.Length < 3) && app.PublishedAt is null) { app.Name = repo.Name; app.Slug = await UniqueSlugAsync(repo.Name, app.Id, ct); }
            if (string.IsNullOrWhiteSpace(app.ShortDescription) && !string.IsNullOrWhiteSpace(repo.Description)) app.ShortDescription = TextUtil.Truncate(repo.Description.Trim(), 200);
            if (!string.IsNullOrWhiteSpace(repo.ReadmeMarkdown))
            {
                app.ReadmeMarkdown = repo.ReadmeMarkdown;
                if (string.IsNullOrWhiteSpace(app.LongDescription)) app.LongDescription = TextUtil.Truncate(repo.ReadmeMarkdown, 20000);
            }
            if (string.IsNullOrWhiteSpace(app.HomepageUrl) && !string.IsNullOrWhiteSpace(repo.Homepage) && Uri.TryCreate(repo.Homepage, UriKind.Absolute, out _)) app.HomepageUrl = repo.Homepage;
            var detected = LicenseDetector.Detect(repo.LicenseText) ?? repo.LicenseSpdxId;
            if (detected is not null)
            {
                var license = await db.Licenses.FirstOrDefaultAsync(l => l.SpdxId == detected && l.IsAllowed, ct);
                if (license is not null) { app.LicenseId = license.Id; app.License = license; }
            }
            if (repo.Topics.Count > 0 && app.AppTags.Count == 0) await SetTagsAsync(app, repo.Topics, ct);
        }
        await lifecycle.RefreshSearchTextAsync(app, ct);
        app.UpdatedAt = Clock.Now;
        jobs.Enqueue(JobTypes.ImportRepository, new { AppId = app.Id, VersionId = version.Id }, $"version:{version.Id}");
        await db.SaveChangesAsync(ct);
        return await MapDraftAsync(app, ct);
    }

    /// <summary>Downloads the repository tarball for the version, stores it as the Source file and analyses it (job handler entry point).</summary>
    public async Task ImportRepositorySnapshotAsync(int appId, int versionId, CancellationToken ct)
    {
        var app = await db.Apps.Include(a => a.License).Include(a => a.Versions).ThenInclude(v => v.Files).FirstOrDefaultAsync(a => a.Id == appId, ct);
        var version = app?.Versions.FirstOrDefault(v => v.Id == versionId);
        if (app is null || version is null || app.RepoUrl is null) return;
        var url = RepositoryUrl.Normalize(app.RepoUrl)!;
        var importer = importers.First(i => i.CanHandle(url));
        try
        {
            var repo = await importer.InspectAsync(url, ct);
            var gitRef = string.IsNullOrWhiteSpace(version.SourceRef) ? repo.DefaultBranch : version.SourceRef;
            var tarballUrl = string.IsNullOrWhiteSpace(version.SourceRef) ? repo.TarballUrl : importer.TarballUrlFor(repo, gitRef);
            var tempPath = Path.Combine(TempDir(), $"import-{versionId}-{Guid.NewGuid():N}.tar.gz");
            try
            {
                using (var client = httpClientFactory.CreateClient("download"))
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("AiAlreadyDidIt/1.0");
                    using var response = await client.GetAsync(tarballUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                    response.EnsureSuccessStatusCode();
                    await using var fs = File.Create(tempPath);
                    await using var body = await response.Content.ReadAsStreamAsync(ct);
                    await CopyLimitedAsync(body, fs, storageOptions.Value.MaxSourceArchiveBytes, ct);
                }
                var fileName = $"{TextUtil.Slugify(repo.Name)}-{TextUtil.Slugify(gitRef)}-source.tar.gz";
                await StoreSourceFromTempAsync(app, version, tempPath, fileName, ct);
                app.RepoStars = repo.Stars;
                app.RepoSyncedAt = Clock.Now;
                if (string.IsNullOrWhiteSpace(version.SourceRef)) version.SourceRef = repo.DefaultBranch;
                await db.SaveChangesAsync(ct);
            }
            finally { TryDelete(tempPath); }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            app.SourceWarnings = TextUtil.Truncate("Import failed: " + ex.Message, 2000);
            await db.SaveChangesAsync(ct);
            throw;
        }
    }

    // ---------------------------------------------------------------- archive upload

    public async Task<AppDraftDto> UploadSourceArchiveAsync(int id, IFormFile file, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var version = app.Versions.OrderByDescending(v => v.Id).FirstOrDefault(v => v.Status is VersionStatus.Draft or VersionStatus.Rejected) ?? throw ApiException.Unprocessable("No draft version.");
        if (!SourceAnalyzer.IsSupportedArchive(file.FileName)) throw ApiException.Unprocessable("Upload a .zip, .tar.gz or .tgz source archive.", "file");
        if (file.Length > storageOptions.Value.MaxSourceArchiveBytes) throw ApiException.Unprocessable($"Source archives are limited to {TextUtil.HumanSize(storageOptions.Value.MaxSourceArchiveBytes)}.", "file");
        if (file.Length == 0) throw ApiException.Unprocessable("The file is empty.", "file");

        var tempPath = Path.Combine(TempDir(), $"src-{version.Id}-{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}");
        try
        {
            await using (var fs = File.Create(tempPath))
            await using (var input = file.OpenReadStream())
                await CopyLimitedAsync(input, fs, storageOptions.Value.MaxSourceArchiveBytes, ct);

            // rule: archive OR repository, never both
            await RemoveSourceFilesAsync(version, ct);
            app.SourceKind = SourceKind.Archive;
            app.RepoUrl = null; app.RepoProvider = null; app.RepoOwner = null; app.RepoName = null; app.RepoDefaultBranch = null; app.RepoStars = null; app.RepoSyncedAt = null;
            version.SourceRef = null;
            await StoreSourceFromTempAsync(app, version, tempPath, Path.GetFileName(file.FileName), ct);
            if ((app.Name == "Untitled app" || app.Name.Length < 3) && app.PublishedAt is null)
            {
                var guess = Path.GetFileNameWithoutExtension(file.FileName.Replace(".tar.gz", ".tgz"));
                if (guess.Length >= 3) { app.Name = guess; app.Slug = await UniqueSlugAsync(guess, app.Id, ct); }
            }
            await lifecycle.RefreshSearchTextAsync(app, ct);
            app.UpdatedAt = Clock.Now;
            await db.SaveChangesAsync(ct);
        }
        finally { TryDelete(tempPath); }
        return await MapDraftAsync(app, ct);
    }

    public async Task<AppDraftDto> RemoveSourceAsync(int id, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var version = app.Versions.OrderByDescending(v => v.Id).FirstOrDefault(v => v.Status is VersionStatus.Draft or VersionStatus.Rejected) ?? throw ApiException.Unprocessable("No draft version.");
        await RemoveSourceFilesAsync(version, ct);
        app.RepoUrl = null; app.RepoProvider = null; app.RepoOwner = null; app.RepoName = null; app.RepoDefaultBranch = null; app.RepoStars = null;
        app.SourceAnalyzedAt = null; app.HasLicenseFile = false; app.DetectedLicenseSpdxId = null; app.SourceWarnings = null; app.SourceFileCount = 0; app.SourceLineCount = 0; app.SourceBytes = 0;
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return await MapDraftAsync(app, ct);
    }

    /// <summary>
    /// "Add an MIT LICENSE for me": rewrites the uploaded source archive with a standard LICENSE file at its root (inside the single
    /// top-level folder when the archive has one) and re-analyses it. Only for archives — a linked repository must get its LICENSE
    /// committed by the owner. Requires the uploader's authorship attestation.
    /// </summary>
    public async Task<AppDraftDto> AddLicenseFileAsync(int id, AddLicenseFileRequest request, CancellationToken ct)
    {
        if (!request.Attest) throw ApiException.Unprocessable("Confirm that the code is your own work before a license can be added to it.", "attest");
        if (!string.Equals(request.SpdxId, "MIT", StringComparison.OrdinalIgnoreCase)) throw ApiException.Unprocessable("Only MIT can be added automatically for now.", "spdxId");
        var app = await LoadOwnedAsync(id, ct);
        var version = app.Versions.OrderByDescending(v => v.Id).FirstOrDefault(v => v.Status is VersionStatus.Draft or VersionStatus.Rejected) ?? throw ApiException.Unprocessable("No draft version.");
        if (app.SourceKind == SourceKind.Repository) throw ApiException.Unprocessable("Add a LICENSE file to the repository itself and re-attach it — the store cannot change your repository.", "source");
        var sourceFile = version.Files.FirstOrDefault(f => f.Kind == FileKind.Source && f.StorageKey is not null) ?? throw ApiException.Unprocessable("Upload the source archive first.", "source");
        if (app.HasLicenseFile) throw ApiException.Unprocessable("The source already contains a LICENSE file.", "source");

        var uploader = await db.Users.AsNoTracking().Where(u => u.Id == app.UploaderUserId).Select(u => new { u.DisplayName, u.Username }).FirstAsync(ct);
        var holder = string.IsNullOrWhiteSpace(uploader.DisplayName) ? uploader.Username : uploader.DisplayName;
        var licenseText = MitLicenseText(Clock.Now.Year, holder);
        var fileName = sourceFile.FileName;
        var isZip = fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        var tempIn = Path.Combine(TempDir(), $"lic-in-{version.Id}-{Guid.NewGuid():N}{(isZip ? ".zip" : ".tar.gz")}");
        var tempOut = Path.Combine(TempDir(), $"lic-out-{version.Id}-{Guid.NewGuid():N}{(isZip ? ".zip" : ".tar.gz")}");
        try
        {
            await using (var src = await storage.OpenReadAsync(Bucket.Sources, sourceFile.StorageKey!, ct))
            await using (var fs = File.Create(tempIn)) await src.CopyToAsync(fs, ct);
            if (isZip) await AddLicenseToZipAsync(tempIn, tempOut, licenseText, ct);
            else await AddLicenseToTarGzAsync(tempIn, tempOut, licenseText, ct);

            await RemoveSourceFilesAsync(version, ct);
            await StoreSourceFromTempAsync(app, version, tempOut, fileName, ct);
            var mit = await db.Licenses.FirstOrDefaultAsync(l => l.SpdxId == "MIT", ct);
            if (mit is not null) { app.LicenseId = mit.Id; app.License = mit; }
            app.AuthorshipAttestedAt ??= Clock.Now;
            app.UpdatedAt = Clock.Now;
            await db.SaveChangesAsync(ct);
            return await MapDraftAsync(app, ct);
        }
        finally { TryDelete(tempIn); TryDelete(tempOut); }
    }

    private static async Task AddLicenseToZipAsync(string input, string output, string licenseText, CancellationToken ct)
    {
        File.Copy(input, output, overwrite: true);
        using var zip = System.IO.Compression.ZipFile.Open(output, System.IO.Compression.ZipArchiveMode.Update);
        var root = CommonRootFolder(zip.Entries.Select(e => e.FullName));
        var entry = zip.CreateEntry(root + "LICENSE", System.IO.Compression.CompressionLevel.Optimal);
        await using var es = entry.Open();
        await using var w = new StreamWriter(es, new System.Text.UTF8Encoding(false));
        await w.WriteAsync(licenseText.AsMemory(), ct);
    }

    private static async Task AddLicenseToTarGzAsync(string input, string output, string licenseText, CancellationToken ct)
    {
        var names = new List<string>();
        await using (var gz = new System.IO.Compression.GZipStream(File.OpenRead(input), System.IO.Compression.CompressionMode.Decompress))
        await using (var reader = new System.Formats.Tar.TarReader(gz))
            while (await reader.GetNextEntryAsync(cancellationToken: ct) is { } e) names.Add(e.Name);
        var root = CommonRootFolder(names);
        await using var outGz = new System.IO.Compression.GZipStream(File.Create(output), System.IO.Compression.CompressionLevel.Optimal);
        await using var writer = new System.Formats.Tar.TarWriter(outGz, System.Formats.Tar.TarEntryFormat.Pax);
        await using (var gz = new System.IO.Compression.GZipStream(File.OpenRead(input), System.IO.Compression.CompressionMode.Decompress))
        await using (var reader = new System.Formats.Tar.TarReader(gz))
            while (await reader.GetNextEntryAsync(copyData: true, cancellationToken: ct) is { } e) await writer.WriteEntryAsync(e, ct);
        var lic = new System.Formats.Tar.PaxTarEntry(System.Formats.Tar.TarEntryType.RegularFile, root + "LICENSE") { DataStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(licenseText)), ModificationTime = DateTimeOffset.UtcNow };
        await writer.WriteEntryAsync(lic, ct);
    }

    /// <summary>"project-1.0/" when every entry lives under one top-level folder (GitHub-style archives), otherwise "".</summary>
    public static string CommonRootFolder(IEnumerable<string> entryNames)
    {
        string? root = null;
        foreach (var raw in entryNames)
        {
            var name = raw.Replace('\\', '/').TrimStart('.', '/');
            if (name.Length == 0) continue;
            var slash = name.IndexOf('/');
            if (slash <= 0) return "";                       // a file at the top level → no common folder
            var top = name[..(slash + 1)];
            if (root is null) root = top; else if (root != top) return "";
        }
        return root ?? "";
    }

    public static string MitLicenseText(int year, string holder) =>
        $"MIT License\n\nCopyright (c) {year} {holder}\n\nPermission is hereby granted, free of charge, to any person obtaining a copy\nof this software and associated documentation files (the \"Software\"), to deal\nin the Software without restriction, including without limitation the rights\nto use, copy, modify, merge, publish, distribute, sublicense, and/or sell\ncopies of the Software, and to permit persons to whom the Software is\nfurnished to do so, subject to the following conditions:\n\nThe above copyright notice and this permission notice shall be included in all\ncopies or substantial portions of the Software.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\nIMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\nFITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\nAUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\nLIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\nOUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE\nSOFTWARE.\n";

    private async Task RemoveSourceFilesAsync(AppVersion version, CancellationToken ct)
    {
        foreach (var f in version.Files.Where(f => f.Kind == FileKind.Source).ToList())
        {
            await DeleteStoredAsync(f, ct);
            version.Files.Remove(f);
            db.AppFiles.Remove(f);
        }
    }

    /// <summary>Analyses the archive on disk, uploads it to the sources bucket and records the Source file on the version.</summary>
    private async Task StoreSourceFromTempAsync(App app, AppVersion version, string tempPath, string fileName, CancellationToken ct)
    {
        SourceAnalysis analysis;
        await using (var fs = File.OpenRead(tempPath))
        {
            try { analysis = await SourceAnalyzer.AnalyzeAsync(fs, fileName, ct); }
            catch (InvalidOperationException ex) { throw ApiException.Unprocessable(ex.Message, "file"); }
            catch (Exception ex) when (ex is InvalidDataException or IOException or FormatException or EndOfStreamException)
            { throw ApiException.Unprocessable("The archive could not be read. Make sure it is a valid .zip or .tar.gz file.", "file"); }
        }
        var info = new FileInfo(tempPath);
        var key = $"{app.Id}/{version.Id}/{Guid.NewGuid():N}/{SafeFileName(fileName)}";
        string sha;
        await using (var fs = File.OpenRead(tempPath))
        {
            sha = Convert.ToHexStringLower(await System.Security.Cryptography.SHA256.HashDataAsync(fs, ct));
        }
        await using (var fs = File.OpenRead(tempPath))
            await storage.PutAsync(Bucket.Sources, key, fs, fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? "application/zip" : "application/gzip", info.Length, ct);

        var sourceFile = new AppFile
        {
            Version = version, Kind = FileKind.Source, FileName = SafeFileName(fileName), StorageKey = key, SizeBytes = info.Length, Sha256 = sha,
            ContentType = fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? "application/zip" : "application/gzip", ScanStatus = ScanStatus.Pending, CreatedAt = Clock.Now
        };
        version.Files.Add(sourceFile);
        db.AppFiles.Add(sourceFile);
        await lifecycle.ApplyAnalysisAsync(app, analysis, ct);
        if (app.LicenseId == 0 || app.License is null || !app.HasLicenseFile) { /* keep declared */ }
        else if (analysis.DetectedLicense is not null && !LicenseDetector.SameFamily(app.License.SpdxId, analysis.DetectedLicense))
        {
            // Prefer what the LICENSE file says when the declared license is still the default.
            var detectedLicense = await db.Licenses.FirstOrDefaultAsync(l => l.SpdxId == analysis.DetectedLicense && l.IsAllowed, ct);
            if (detectedLicense is not null && app.PublishedAt is null && app.Status == AppStatus.Draft && app.SubmittedAt is null) { app.LicenseId = detectedLicense.Id; app.License = detectedLicense; }
        }
    }

    internal static string SafeFileName(string name)
    {
        var baseName = Path.GetFileName(name.Replace('\\', '/'));
        var cleaned = new string(baseName.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' or '+' ? c : '_').ToArray()).Trim('.');
        return string.IsNullOrEmpty(cleaned) ? "file" : TextUtil.Truncate(cleaned, 200);
    }

    private static async Task CopyLimitedAsync(Stream input, Stream output, long maxBytes, CancellationToken ct)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, ct)) > 0)
        {
            total += read;
            if (total > maxBytes) throw ApiException.Unprocessable($"The file exceeds the limit of {TextUtil.HumanSize(maxBytes)}.", "file");
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
