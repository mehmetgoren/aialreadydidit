using AiAlreadyDidIt.Api.Infrastructure.Import;

namespace AiAlreadyDidIt.Tests;

public class LicenseDetectorTests
{
    public const string Mit = """
        MIT License

        Copyright (c) 2026 CPU-Z for Linux contributors

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
        """;

    [Fact]
    public void Detects_mit_from_the_seed_license_text() => Assert.Equal("MIT", LicenseDetector.Detect(Mit));

    [Fact]
    public void Detects_mit_zero_when_the_notice_clause_is_missing()
    {
        var text = "MIT No Attribution\n\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software to deal in the Software without restriction.";
        Assert.Equal("MIT-0", LicenseDetector.Detect(text));
    }

    [Theory]
    [InlineData("Apache License\nVersion 2.0, January 2004\nhttp://www.apache.org/licenses/", "Apache-2.0")]
    [InlineData("GNU GENERAL PUBLIC LICENSE\nVersion 3, 29 June 2007", "GPL-3.0-only")]
    [InlineData("GNU GENERAL PUBLIC LICENSE Version 3 ... either version 3 of the License, or (at your option) any later version.", "GPL-3.0-or-later")]
    [InlineData("GNU GENERAL PUBLIC LICENSE\nVersion 2, June 1991", "GPL-2.0-only")]
    [InlineData("GNU LESSER GENERAL PUBLIC LICENSE\nVersion 3, 29 June 2007", "LGPL-3.0-only")]
    [InlineData("GNU LESSER GENERAL PUBLIC LICENSE Version 2.1, February 1999", "LGPL-2.1-only")]
    [InlineData("GNU AFFERO GENERAL PUBLIC LICENSE\nVersion 3, 19 November 2007", "AGPL-3.0-only")]
    [InlineData("Mozilla Public License Version 2.0", "MPL-2.0")]
    [InlineData("This is free and unencumbered software released into the public domain.", "Unlicense")]
    [InlineData("CC0 1.0 Universal", "CC0-1.0")]
    [InlineData("Boost Software License - Version 1.0", "BSL-1.0")]
    [InlineData("Attribution-ShareAlike 4.0 International", "CC-BY-SA-4.0")]
    [InlineData("Attribution 4.0 International", "CC-BY-4.0")]
    [InlineData("Redistribution and use in source and binary forms, with or without modification, are permitted provided that ... Neither the name of the copyright holder nor the names of its contributors may be used", "BSD-3-Clause")]
    [InlineData("Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met", "BSD-2-Clause")]
    [InlineData("Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby granted, provided that the above copyright notice and this permission notice appear in all copies.", "ISC")]
    [InlineData("Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby granted.", "0BSD")]
    public void Detects_common_licenses(string text, string expected)
    {
        Assert.Equal(expected, LicenseDetector.Detect(text));
    }

    [Fact]
    public void Detection_ignores_whitespace_and_case()
    {
        var mangled = Mit.ToUpperInvariant().Replace("\n", "   \r\n\t ");
        Assert.Equal("MIT", LicenseDetector.Detect(mangled));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("All rights reserved. Do not copy.")]
    public void Returns_null_for_unknown_or_empty(string? text)
    {
        Assert.Null(LicenseDetector.Detect(text));
    }

    [Theory]
    [InlineData("GPL-3.0-only", "GPL-3.0")]
    [InlineData("GPL-3.0-or-later", "GPL-3.0")]
    [InlineData("MIT-0", "MIT")]
    [InlineData("Apache-2.0", "Apache-2.0")]
    public void Family_strips_only_or_later_suffixes(string id, string family)
    {
        Assert.Equal(family, LicenseDetector.Family(id));
    }

    [Fact]
    public void SameFamily_is_case_insensitive_and_suffix_tolerant()
    {
        Assert.True(LicenseDetector.SameFamily("gpl-3.0-only", "GPL-3.0-or-later"));
        Assert.True(LicenseDetector.SameFamily("MIT", "MIT-0"));
        Assert.False(LicenseDetector.SameFamily("MIT", "Apache-2.0"));
        Assert.False(LicenseDetector.SameFamily("GPL-2.0-only", "GPL-3.0-only"));
    }
}
