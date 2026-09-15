using AiAlreadyDidIt.Api.Services.Apps;

namespace AiAlreadyDidIt.Tests;

public class AddLicenseFileTests
{
    [Fact]
    public void Common_root_folder_is_detected_for_github_style_archives()
    {
        Assert.Equal("proj-1.0/", AppEditorService.CommonRootFolder(["proj-1.0/", "proj-1.0/src/main.py", "proj-1.0/README.md"]));
        Assert.Equal("proj/", AppEditorService.CommonRootFolder(["./proj/a.txt", "proj/b/c.txt"]));
    }

    [Fact]
    public void No_common_root_when_files_sit_at_the_top_or_folders_differ()
    {
        Assert.Equal("", AppEditorService.CommonRootFolder(["README.md", "src/main.py"]));
        Assert.Equal("", AppEditorService.CommonRootFolder(["a/x.py", "b/y.py"]));
        Assert.Equal("", AppEditorService.CommonRootFolder([]));
    }

    [Fact]
    public void Mit_text_is_the_standard_license_with_year_and_holder()
    {
        var text = AppEditorService.MitLicenseText(2026, "Mehmet");
        Assert.StartsWith("MIT License", text);
        Assert.Contains("Copyright (c) 2026 Mehmet", text);
        Assert.Contains("Permission is hereby granted, free of charge, to any person obtaining a copy", text);
        Assert.Contains("The above copyright notice and this permission notice shall be included", text);
        Assert.Equal("MIT", Api.Infrastructure.Import.LicenseDetector.Detect(text));
    }
}
