using System.Text.RegularExpressions;

namespace AiAlreadyDidIt.Api.Infrastructure.Import;

/// <summary>Recognises the common open-source licenses from their text (distinctive phrases, whitespace-insensitive).</summary>
public static partial class LicenseDetector
{
    [GeneratedRegex(@"\s+")] private static partial Regex Ws();

    private static string Norm(string text) => Ws().Replace(text, " ").ToLowerInvariant().Replace('’', '\'').Replace("“", "\"").Replace("”", "\"");

    private sealed record Rule(string SpdxId, string[] All, string[]? Any = null, string[]? None = null);

    private static readonly Rule[] Rules =
    [
        new("AGPL-3.0-only", ["gnu affero general public license", "version 3"]),
        new("LGPL-3.0-only", ["gnu lesser general public license", "version 3"]),
        new("LGPL-2.1-only", ["gnu lesser general public license", "version 2.1"]),
        new("LGPL-2.1-only", ["gnu library general public license", "version 2"]),
        new("GPL-3.0-only", ["gnu general public license", "version 3"], None: ["lesser", "affero"]),
        new("GPL-2.0-only", ["gnu general public license", "version 2"], None: ["lesser", "affero"]),
        new("Apache-2.0", ["apache license", "version 2.0"]),
        new("MPL-2.0", ["mozilla public license", "2.0"]),
        new("EPL-2.0", ["eclipse public license", "2.0"]),
        new("EUPL-1.2", ["european union public licence", "1.2"]),
        new("EUPL-1.2", ["european union public license", "1.2"]),
        new("BSL-1.0", ["boost software license"]),
        new("Unlicense", ["this is free and unencumbered software released into the public domain"]),
        new("CC0-1.0", ["cc0 1.0 universal"]),
        new("CC0-1.0", ["creative commons zero"]),
        new("CC-BY-SA-4.0", ["attribution-sharealike 4.0"]),
        new("CC-BY-NC-4.0", ["attribution-noncommercial 4.0"]),
        new("CC-BY-4.0", ["attribution 4.0 international"], None: ["sharealike", "noncommercial"]),
        new("WTFPL", ["do what the fuck you want to public license"]),
        new("Zlib", ["this software is provided 'as-is', without any express or implied warranty", "altered source versions must be plainly marked as such"]),
        new("PostgreSQL", ["postgresql license"]),
        new("Artistic-2.0", ["artistic license 2.0"]),
        new("NCSA", ["university of illinois/ncsa open source license"]),
        new("UPL-1.0", ["universal permissive license"]),
        new("OSL-3.0", ["open software license", "3.0"]),
        new("SSPL-1.0", ["server side public license"]),
        new("BUSL-1.1", ["business source license"]),
        new("MulanPSL-2.0", ["mulan permissive software license"]),
        new("ISC", ["permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby granted"]),
        new("0BSD", ["permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby granted"], None: ["provided that the above copyright notice and this permission notice appear in all copies"]),
        new("BSD-3-Clause", ["redistribution and use in source and binary forms", "neither the name of"]),
        new("BSD-2-Clause", ["redistribution and use in source and binary forms"], None: ["neither the name of"]),
        new("MIT-0", ["permission is hereby granted, free of charge, to any person obtaining a copy"], None: ["the above copyright notice and this permission notice shall be included"]),
        new("MIT", ["permission is hereby granted, free of charge, to any person obtaining a copy", "the above copyright notice and this permission notice shall be included"]),
    ];

    /// <summary>Returns the SPDX id of the first rule matching the text, or null.</summary>
    public static string? Detect(string? licenseText)
    {
        if (string.IsNullOrWhiteSpace(licenseText)) return null;
        var text = Norm(licenseText);
        foreach (var rule in Rules)
        {
            if (!rule.All.All(text.Contains)) continue;
            if (rule.Any is not null && !rule.Any.Any(text.Contains)) continue;
            if (rule.None is not null && rule.None.Any(text.Contains)) continue;
            var id = rule.SpdxId;
            // "-only" vs "-or-later": look for the "any later version" clause.
            if (id.EndsWith("-only", StringComparison.Ordinal) && text.Contains("any later version"))
                id = id[..^"-only".Length] + "-or-later";
            return id;
        }
        return null;
    }

    /// <summary>GPL-3.0-only / GPL-3.0-or-later → GPL-3.0; used to compare declared vs detected.</summary>
    public static string Family(string spdxId)
    {
        var id = spdxId;
        foreach (var suffix in new[] { "-only", "-or-later" })
            if (id.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) id = id[..^suffix.Length];
        if (id.Equals("MIT-0", StringComparison.OrdinalIgnoreCase)) return "MIT";
        return id;
    }

    public static bool SameFamily(string declared, string detected) =>
        string.Equals(Family(declared), Family(detected), StringComparison.OrdinalIgnoreCase);
}
