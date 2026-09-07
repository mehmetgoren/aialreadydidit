using System.Text;
using AiAlreadyDidIt.Api.Infrastructure.Import;

namespace AiAlreadyDidIt.Tests;

public class InstallerSignatureTests
{
    private static ReadOnlyMemory<byte> Bytes(params byte[] b) => b;
    private static ReadOnlyMemory<byte> Text(string s) => Encoding.UTF8.GetBytes(s);
    private static ReadOnlyMemory<byte> Zip() => Bytes(0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00);

    private static ReadOnlyMemory<byte> Tar()
    {
        var b = new byte[512];
        Encoding.ASCII.GetBytes("ustar").CopyTo(b, 257);
        return b;
    }

    [Theory]
    [InlineData("setup.exe", new byte[] { 0x4D, 0x5A, 0x90, 0x00 })]
    [InlineData("setup.msi", new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 })]
    [InlineData("app.rpm", new byte[] { 0xED, 0xAB, 0xEE, 0xDB, 0x03 })]
    [InlineData("app.tar.gz", new byte[] { 0x1F, 0x8B, 0x08 })]
    [InlineData("app.tgz", new byte[] { 0x1F, 0x8B, 0x08 })]
    [InlineData("app.tar.xz", new byte[] { 0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00 })]
    [InlineData("App-x86_64.AppImage", new byte[] { 0x7F, 0x45, 0x4C, 0x46, 0x02 })]
    public void Accepts_matching_binary_headers(string name, byte[] head) => Assert.Null(InstallerSignature.Check(name, head));

    [Theory]
    [InlineData("app.zip")]
    [InlineData("App.app.zip")]
    [InlineData("app.msix")]
    [InlineData("app.appx")]
    [InlineData("app.apk")]
    [InlineData("app.aab")]
    [InlineData("app.ipa")]
    [InlineData("tool.jar")]
    [InlineData("pkg-1.0-py3-none-any.whl")]
    public void Accepts_zip_based_formats(string name) => Assert.Null(InstallerSignature.Check(name, Zip()));

    [Fact]
    public void Accepts_empty_zip_end_record() => Assert.Null(InstallerSignature.Check("a.zip", Bytes(0x50, 0x4B, 0x05, 0x06, 0, 0)));

    [Fact]
    public void Accepts_deb_ar_archive() => Assert.Null(InstallerSignature.Check("app_1.0_amd64.deb", Text("!<arch>\ndebian-binary   ")));

    [Fact]
    public void Accepts_pkg_xar() => Assert.Null(InstallerSignature.Check("App.pkg", Text("xar!\0\x1c")));

    [Fact]
    public void Accepts_snap_squashfs() => Assert.Null(InstallerSignature.Check("app.snap", Text("hsqs")));

    [Theory]
    [InlineData("image.tar")]
    [InlineData("lib.gem")]
    public void Accepts_tar_with_ustar_marker(string name) => Assert.Null(InstallerSignature.Check(name, Tar()));

    [Fact]
    public void Accepts_iso_appimage()
    {
        var b = new byte[0x8001 + 5];
        Encoding.ASCII.GetBytes("CD001").CopyTo(b, 0x8001);
        Assert.Null(InstallerSignature.Check("old.AppImage", b));
    }

    [Theory]
    [InlineData("install.sh", "#!/bin/sh\necho hi\n")]
    [InlineData("install.ps1", "Write-Host 'hi'\r\n")]
    [InlineData("tool.py", "print('hi')\n")]
    [InlineData("tool.js", "console.log('hi')\n")]
    [InlineData("setup.run", "#!/bin/sh\n# makeself\n")]
    public void Accepts_text_scripts(string name, string body) => Assert.Null(InstallerSignature.Check(name, Text(body)));

    [Fact]
    public void Accepts_utf8_bom_script() => Assert.Null(InstallerSignature.Check("a.ps1", Bytes(0xEF, 0xBB, 0xBF, (byte)'W', (byte)'r')));

    [Fact]
    public void Accepts_elf_run_installer() => Assert.Null(InstallerSignature.Check("setup.run", Bytes(0x7F, 0x45, 0x4C, 0x46)));

    [Theory]
    [InlineData("App.dmg")]
    [InlineData("app.flatpak")]
    public void Formats_without_a_header_pass_unless_web_page(string name)
    {
        Assert.Null(InstallerSignature.Check(name, Bytes(0x01, 0x02, 0x03, 0x04, 0x00)));
        Assert.Contains("web page", InstallerSignature.Check(name, Text("<!DOCTYPE html><html>")));
    }

    [Theory]
    [InlineData("setup.exe", "<!DOCTYPE html>")]
    [InlineData("app.zip", "  <html lang=\"en\">")]
    [InlineData("app.deb", "﻿<html>")]
    [InlineData("install.sh", "<script>alert(1)</script>")]
    [InlineData("index.py", "<?php echo 1; ?>")]
    public void Rejects_web_pages(string name, string body) => Assert.Contains("web page", InstallerSignature.Check(name, Text(body)));

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("app.msi")]
    [InlineData("app.deb")]
    [InlineData("app.rpm")]
    [InlineData("app.zip")]
    [InlineData("app.apk")]
    [InlineData("app.tar.gz")]
    [InlineData("app.AppImage")]
    public void Rejects_plain_text_for_binary_formats(string name)
    {
        var problem = InstallerSignature.Check(name, Text("This is just a readme pretending to be an installer.\n"));
        Assert.NotNull(problem);
        Assert.Contains("plain text", problem);
    }

    [Fact]
    public void Rejects_wrong_binary_header()
    {
        var problem = InstallerSignature.Check("setup.exe", Zip());
        Assert.NotNull(problem);
        Assert.Contains("Windows executable", problem);
    }

    [Fact]
    public void Rejects_binary_as_script()
    {
        var problem = InstallerSignature.Check("install.sh", Bytes(0x7F, 0x45, 0x4C, 0x46, 0x00, 0x00));
        Assert.NotNull(problem);
        Assert.Contains("shell script", problem);
    }

    [Fact]
    public void Rejects_tar_without_ustar()
    {
        var b = new byte[600];
        Assert.NotNull(InstallerSignature.Check("image.tar", b));
    }

    [Fact]
    public void Rejects_empty() => Assert.Equal("The file is empty.", InstallerSignature.Check("a.exe", ReadOnlyMemory<byte>.Empty));

    [Fact]
    public void Extension_matching_is_case_insensitive() => Assert.Null(InstallerSignature.Check("SETUP.EXE", Bytes(0x4D, 0x5A)));

    [Fact]
    public void IsText_rejects_nul_bytes() => Assert.False(InstallerSignature.IsText(Bytes((byte)'a', 0x00, (byte)'b')));
}
