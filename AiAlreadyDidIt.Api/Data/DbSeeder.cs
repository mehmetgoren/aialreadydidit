using System.Formats.Tar;
using System.IO.Compression;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Import;
using AiAlreadyDidIt.Api.Infrastructure.Jobs;
using AiAlreadyDidIt.Api.Infrastructure.Storage;
using AiAlreadyDidIt.Api.Services.Apps;
using AiAlreadyDidIt.Api.Services.Catalog;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Data;

/// <summary>
/// Development seed: a demo member plus the first two real LLM-generated apps (CPU-Z for Linux, HWMonitor for Linux)
/// taken from <c>Seed:LlmProjectsPath</c>. Files go through the real pipeline (source snapshot, analysis, object
/// storage, antivirus job, embedding job). Runs once, only while the <c>apps</c> table is empty.
/// </summary>
public sealed class DbSeeder(AadiDbContext db, IPasswordHasher hasher, IObjectStorage storage, AppLifecycleService lifecycle, CategoryIndexService categories,
    JobQueue jobs, IConfiguration configuration, ILogger<DbSeeder> logger)
{
    public const string DemoUsername = "demo";

    private sealed record SeedApp(string Folder, string Name, string Slug, string Short, string CategorySlug, string[] Tags, string[] Screenshots, string Installer, string Version,
        string Changelog, string ModelSlug, string ModelNote, (string Title, string Text)[] Prompts, string? DerivedFromSlug = null);

    /// <param name="sampleAppsOnly">
    /// Production mode (<c>Seed:PublishSampleApps</c>): publish the two real apps under the admin account with current
    /// timestamps, but no demo member, no fake downloads / ratings. Idempotent — skipped once any app exists.
    /// </param>
    public async Task SeedAsync(bool sampleAppsOnly = false, CancellationToken ct = default)
    {
        if (await db.Apps.AnyAsync(ct)) { logger.LogInformation("Apps already seeded; skipping."); return; }
        var root = configuration["Seed:LlmProjectsPath"];
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) { logger.LogWarning("Seed:LlmProjectsPath '{Path}' not found; skipping demo apps.", root); return; }

        var admin = await db.Users.Include(u => u.Role).OrderBy(u => u.Id).FirstAsync(u => u.Role.IsAdmin, ct);
        var demo = sampleAppsOnly ? null : await EnsureDemoMemberAsync(ct);

        var cpuz = new SeedApp("cpu_z", "CPU-Z for Linux", "cpuz-linux",
            "Unofficial CPU-Z re-implementation for Linux Mint / GTK 3: CPU, caches, mainboard, memory, SPD, graphics, bench and validation tabs filled from the kernel.",
            "system-and-utilities-hardware-and-sensors-system-information",
            ["cpu-z", "hardware", "system-information", "gtk3", "linux-mint", "python", "benchmark", "spd", "dmi"],
            ["docs/cpu.png", "docs/caches.png", "docs/mainboard.png", "docs/memory.png", "docs/spd.png", "docs/graphics.png", "docs/bench.png", "docs/bench_result.png", "docs/daemon_running.png", "docs/about.png"],
            "dist/cpuz-linux_1.0.0_all.deb", "1.0.0",
            "Initial release: CPU / Caches / Mainboard / Memory / SPD / Graphics / Bench / About tabs, live temperature, privileged daemon (polkit) for SPD and core voltage, Debian package.",
            "anthropic-claude-fable-5-1", "Claude Code session; prompts below were reconstructed from the README",
            [
                ("Initial prompt", "Re-implement CPUID's CPU-Z for Linux Mint (GTK 3, Python). Reproduce the CPU-Z window exactly: the eight tabs CPU, Caches, Mainboard, Memory, SPD, Graphics, Bench, About with the navy value boxes, etched group boxes and the Tools / Validate / Close bar. Fill every field from the Linux kernel instead of a Windows driver: CPUID instruction, /proc/cpuinfo, cpufreq, hwmon (k10temp/coretemp), sysfs cache topology, DMI (/sys/class/dmi), PCI ids, SPD EEPROM via ee1004/spd5118, DRM/sysfs and nvidia-smi for GPUs. Include a built-in code-name / TDP / GPU database keyed by CPUID family/model."),
                ("Benchmark & daemon", "Add a Bench tab with single- and multi-thread CPU benchmark calibrated to the CPU-Z 17 score scale with a reference list from the CPU-Z Validator ranking, plus a stress test. Add a privileged helper started through pkexec (polkit policy) that answers 'collect' (dmidecode tables and raw SPD dumps), 'voltage' (VID MSR) and 'ping' over a private Unix socket, exits when the app closes. Add Tools menu items: save report as TXT/HTML, Clocks window, Timers, Start/Stop daemon, Save validation file (F7), Screenshot (F5), Copy page (F6). Same command-line switches as CPU-Z (-txt, -html, -core, -console)."),
                ("Packaging", "Provide install.sh (system-wide or --user), a desktop entry, and packaging/build-deb.sh that builds dist/cpuz-linux_<version>_all.deb installing to /usr/lib/cpuz-linux with the polkit policy. PyGObject must come from the distro (python3-gi), so uv/venv setups must use --system-site-packages.")
            ]);

        var hw = new SeedApp("hw_monitor", "HWMonitor for Linux", "hwmonitor-linux",
            "Hardware monitoring for Linux Mint that reproduces CPUID HWMonitor: sensor tree with value/min/max, CSV logging, real-time graphs, four themes, tray icon.",
            "system-and-utilities-hardware-and-sensors-hardware-monitoring",
            ["hwmonitor", "hardware", "sensors", "monitoring", "gtk3", "linux-mint", "python", "temperatures", "graphs"],
            ["screenshots/classic.png", "screenshots/dark.png", "screenshots/graphs.png"],
            "dist/hwmonitor-linux_1.0.0_all.deb", "1.0.0",
            "Initial release: device/sensor tree (mainboard, CPU, memory, GPUs, drives, batteries), min/max tracking, CSV logging (F5), graph window, four themes, Options dialog, Sensor Setup Hints, Debian package.",
            "anthropic-claude-fable-5-1", "Claude Code session; prompts below were reconstructed from the README",
            [
                ("Initial prompt", "Write a hardware monitoring program for Linux Mint (Cinnamon / GTK 3, Python) that reproduces the look and feature set of CPUID HWMonitor on Windows. Devices form a tree with Sensor / Value / Min / Max columns and sensor groups per device: mainboard (Super-IO voltages/temperatures/fans, NIC temps, ACPI thermal zones), CPU (voltages, temperatures incl. per core, RAPL powers, per-core clocks marking preferred cores, utilization), memory, AMD/Intel/NVIDIA GPUs, NVMe/SATA/USB drives (temperatures, used space, activity, read/write rate), batteries. Read everything from /sys/class/hwmon, /sys/class/thermal, powercap, cpufreq, /proc/stat, /proc/meminfo, amdgpu/i915 sysfs, nvidia-smi, /sys/block, statvfs and /sys/class/power_supply. Track min/max since start with a Clear Min/Max action."),
                ("Features", "Match HWMonitor's features: File → Save Monitoring Data (text report), F5 CSV data logging with a status-bar indicator, right-click a sensor → Add to Graph opening a real-time graph window (sensors of the same type share a graph, up to 8 curves, settings dialog with big font, curve colour, delete, move), View → Theme (Default classic light with alternating rows, Dark purple, Dark #2 orange, Blue), Expand/Collapse All, Always On Top, Tools → Options (refresh interval, °C/°F, log folder, tray icon), Tools → Sensor Setup Hints listing kernel modules or privileges that would unlock more sensors. Remember window size, column widths, collapsed rows, theme and graphs in ~/.config/hwmonitor-linux/config.json."),
                ("Packaging", "Provide hwmonitor.py launcher, install.sh / uninstall.sh (menu entry + hwmonitor-linux command, uv venv on the system Python with --system-site-packages) and build-deb.sh producing dist/hwmonitor-linux_<version>_all.deb that pulls in the GTK/PyGObject dependencies. MIT license.")
            ]);

        var index = await categories.GetAsync(ct);
        foreach (var seed in new[] { cpuz, hw })
        {
            try { await SeedAppAsync(seed, admin, index, root, backdate: !sampleAppsOnly, ct); }
            catch (Exception ex) { logger.LogError(ex, "Seeding {App} failed", seed.Name); }
        }
        if (demo is not null) await SeedEngagementAsync(demo, ct);
        await lifecycle.RecomputeReferenceCountsAsync(ct);
        logger.LogInformation("Seed completed.");
    }

    private async Task<User> EnsureDemoMemberAsync(CancellationToken ct)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Username == DemoUsername, ct);
        if (existing is not null) return existing;
        var role = await db.Roles.FirstAsync(r => r.Name == ReferenceDataSeeder.MemberRoleName, ct);
        var demo = new User
        {
            Email = "demo@example.com", EmailVerifiedAt = Clock.Now, Username = DemoUsername, DisplayName = "Demo Member",
            PasswordHash = hasher.Hash(configuration["Seed:DemoMemberPassword"] ?? "Aadi123!"), RoleId = role.Id, IsActive = true, Locale = "en-US",
            Bio = "Demo account created by the development seed.", CreatedAt = Clock.Now.AddDays(-20), UpdatedAt = Clock.Now
        };
        db.Users.Add(demo);
        await db.SaveChangesAsync(ct);
        return demo;
    }

    private async Task SeedAppAsync(SeedApp seed, User uploader, CategoryIndexService.CategoryIndex index, string root, bool backdate, CancellationToken ct)
    {
        var dir = Path.Combine(root, seed.Folder);
        if (!Directory.Exists(dir)) { logger.LogWarning("Seed folder {Dir} missing", dir); return; }
        var category = index.BySlug.TryGetValue(seed.CategorySlug, out var node) ? node : index.BySlug["other-uncategorised"];
        var readme = File.Exists(Path.Combine(dir, "README.md")) ? await File.ReadAllTextAsync(Path.Combine(dir, "README.md"), ct) : null;
        var model = await db.LlmModels.FirstOrDefaultAsync(m => m.Slug == seed.ModelSlug, ct);
        var license = await db.Licenses.FirstAsync(l => l.SpdxId == "MIT", ct);
        var linux = await db.Platforms.FirstAsync(p => p.Code == "linux", ct);
        var now = Clock.Now;
        // Development backdates the timeline so the storefront looks lived-in; production publishes "now".
        DateTime Ago(int days) => backdate ? now.AddDays(-days) : now;

        var app = new App
        {
            Slug = seed.Slug, Name = seed.Name, ShortDescription = TextUtil.Truncate(seed.Short, 200), LongDescription = readme is null ? seed.Short : TextUtil.Truncate(readme, 20000), ReadmeMarkdown = readme,
            CategoryId = category.Id, LicenseId = license.Id, License = license, UploaderUserId = uploader.Id, LlmModelId = model?.Id, LlmModelNote = seed.ModelNote,
            SourceKind = SourceKind.Archive, Status = AppStatus.Published, SubmittedAt = Ago(3), PublishedAt = Ago(2), TagsText = string.Empty, CategoryPathText = string.Empty,
            CreatedAt = Ago(4), UpdatedAt = Ago(2), IsFeatured = true, FeaturedOrder = seed.Slug == "cpuz-linux" ? 1 : 2
        };
        if (seed.DerivedFromSlug is not null) app.DerivedFromAppId = await db.Apps.Where(a => a.Slug == seed.DerivedFromSlug).Select(a => (int?)a.Id).FirstOrDefaultAsync(ct);
        var version = new AppVersion { Version = seed.Version, Changelog = seed.Changelog, ReleasedAt = Ago(2), Status = VersionStatus.Published, CreatedByUserId = uploader.Id, CreatedAt = Ago(3), PublishedAt = Ago(2) };
        app.Versions.Add(version);
        var order = 0;
        foreach (var (title, text) in seed.Prompts) app.Prompts.Add(new AppPrompt { Title = title, PromptText = text, SortOrder = order++ });
        db.Apps.Add(app);
        await db.SaveChangesAsync(ct);

        // tags
        foreach (var name in seed.Tags)
        {
            var slug = TextUtil.Slugify(name, 48);
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Slug == slug, ct) ?? db.Tags.Add(new Tag { Name = name, Slug = slug }).Entity;
            app.AppTags.Add(new AppTag { App = app, Tag = tag });
        }
        await db.SaveChangesAsync(ct);

        // source snapshot (tar.gz of the working tree without junk) → analysis → sources bucket
        var tempTar = Path.Combine(Path.GetTempPath(), $"aadi-seed-{seed.Slug}.tar.gz");
        await BuildTarballAsync(dir, seed.Slug, tempTar, ct);
        SourceAnalysis analysis;
        await using (var fs = File.OpenRead(tempTar)) analysis = await SourceAnalyzer.AnalyzeAsync(fs, tempTar, ct);
        var sourceKey = $"{app.Id}/{version.Id}/{Guid.NewGuid():N}/{seed.Slug}-{seed.Version}-source.tar.gz";
        var tarInfo = new FileInfo(tempTar);
        string tarSha;
        await using (var fs = File.OpenRead(tempTar)) tarSha = Convert.ToHexStringLower(await System.Security.Cryptography.SHA256.HashDataAsync(fs, ct));
        await using (var fs = File.OpenRead(tempTar)) await storage.PutAsync(Bucket.Sources, sourceKey, fs, "application/gzip", tarInfo.Length, ct);
        version.Files.Add(new AppFile { Kind = FileKind.Source, FileName = $"{seed.Slug}-{seed.Version}-source.tar.gz", StorageKey = sourceKey, SizeBytes = tarInfo.Length, Sha256 = tarSha, ContentType = "application/gzip", ScanStatus = ScanStatus.Pending, CreatedAt = now });
        File.Delete(tempTar);
        await lifecycle.ApplyAnalysisAsync(app, analysis, ct);

        // installer (.deb) → installers bucket
        var installerPath = Path.Combine(dir, seed.Installer);
        if (File.Exists(installerPath))
        {
            var info = new FileInfo(installerPath);
            string sha;
            await using (var fs = File.OpenRead(installerPath)) sha = Convert.ToHexStringLower(await System.Security.Cryptography.SHA256.HashDataAsync(fs, ct));
            var key = $"{app.Id}/{version.Id}/{Guid.NewGuid():N}/{info.Name}";
            await using (var fs = File.OpenRead(installerPath)) await storage.PutAsync(Bucket.Installers, key, fs, "application/vnd.debian.binary-package", info.Length, ct);
            version.Files.Add(new AppFile { Kind = FileKind.Installer, PlatformId = linux.Id, FileName = info.Name, StorageKey = key, SizeBytes = info.Length, Sha256 = sha, ContentType = "application/vnd.debian.binary-package", ScanStatus = ScanStatus.Pending, InstallHint = $"sudo apt install ./{info.Name}", CreatedAt = now });
        }

        // screenshots → webp + thumbnails
        var sort = 0;
        foreach (var rel in seed.Screenshots)
        {
            var path = Path.Combine(dir, rel);
            if (!File.Exists(path)) continue;
            await using var input = File.OpenRead(path);
            var (full, thumb) = await ImageProcessor.ProcessScreenshotAsync(input, ct);
            var baseKey = $"{app.Id}/{Guid.NewGuid():N}";
            await storage.PutAsync(Bucket.Screenshots, baseKey + ".webp", new MemoryStream(full.Bytes), full.ContentType, full.Bytes.Length, ct);
            await storage.PutAsync(Bucket.Screenshots, baseKey + "-thumb.webp", new MemoryStream(thumb.Bytes), thumb.ContentType, thumb.Bytes.Length, ct);
            app.Screenshots.Add(new AppScreenshot { StorageKey = baseKey + ".webp", ThumbStorageKey = baseKey + "-thumb.webp", Width = full.Width, Height = full.Height, Caption = Path.GetFileNameWithoutExtension(rel).Replace('_', ' '), SortOrder = sort++, CreatedAt = now });
        }

        app.LatestVersionId = version.Id;
        await lifecycle.RefreshSearchTextAsync(app, ct);
        await db.SaveChangesAsync(ct);
        jobs.Enqueue(JobTypes.ScanVersion, new { AppId = app.Id, VersionId = version.Id }, $"version:{version.Id}");
        jobs.Enqueue(JobTypes.EmbedApp, new { AppId = app.Id }, $"app:{app.Id}");
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {App}: {Lines} lines, {Files} files, license {License}", seed.Name, analysis.LineCount, analysis.SourceFileCount, analysis.DetectedLicense);
    }

    private static async Task BuildTarballAsync(string dir, string topFolder, string outputPath, CancellationToken ct)
    {
        string[] skip = [".venv", ".idea", ".git", "dist", "__pycache__", ".pytest_cache", "node_modules"];
        await using var output = File.Create(outputPath);
        await using var gzip = new GZipStream(output, CompressionLevel.Optimal);
        await using var writer = new TarWriter(gzip, TarEntryFormat.Pax, leaveOpen: false);
        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(dir, file).Replace('\\', '/');
            if (rel.Split('/').Any(seg => skip.Contains(seg))) continue;
            var entry = new PaxTarEntry(TarEntryType.RegularFile, $"{topFolder}/{rel}") { DataStream = File.OpenRead(file) };
            await writer.WriteEntryAsync(entry, ct);
            await entry.DataStream.DisposeAsync();
        }
    }

    /// <summary>Demo member downloads and rates both apps so the storefront shows ratings and the counter moves.</summary>
    private async Task SeedEngagementAsync(User demo, CancellationToken ct)
    {
        var apps = await db.Apps.Include(a => a.Versions).ThenInclude(v => v.Files).Where(a => a.Status == AppStatus.Published).ToListAsync(ct);
        if (apps.Count == 0) return;
        var rnd = new Random(20260905);
        foreach (var app in apps)
        {
            var version = app.Versions.OrderBy(v => v.Id).First();
            var installer = version.Files.FirstOrDefault(f => f.Kind == FileKind.Installer) ?? version.Files.First();
            var count = 3 + rnd.Next(4);
            for (var i = 0; i < count; i++)
                db.Downloads.Add(new Download { AppId = app.Id, VersionId = version.Id, FileId = installer.Id, UserId = i == 0 ? demo.Id : null, Source = i % 3 == 0 ? DownloadSource.Api : DownloadSource.Web, CreatedAt = Clock.Now.AddHours(-rnd.Next(1, 40)) });
            db.Ratings.Add(new Rating
            {
                AppId = app.Id, UserId = demo.Id, Score = 85 + rnd.Next(12), VersionId = version.Id, Worked = true,
                Review = app.Slug == "cpuz-linux" ? "Installed the .deb on Mint 22 and every tab filled in — even SPD after starting the daemon. Bench numbers line up with the CPU-Z validator." : "Looks exactly like HWMonitor. Graphs and CSV logging work; had to modprobe nct6775 for board fans as the hints suggested.",
                CreatedAt = Clock.Now.AddHours(-6), UpdatedAt = Clock.Now.AddHours(-6)
            });
            db.AppWatches.Add(new AppWatch { UserId = demo.Id, AppId = app.Id, CreatedAt = Clock.Now });
        }
        db.Favorites.Add(new Favorite { UserId = demo.Id, AppId = apps.First().Id, CreatedAt = Clock.Now });
        await db.SaveChangesAsync(ct);
        foreach (var app in apps) await lifecycle.RecomputeCountersAsync(app.Id, ct);
    }
}
