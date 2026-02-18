static class ExtensionPackageResolver
{
    static readonly string[] frameworkSuffixes =
    [
        ".MSTest",
        ".NUnit",
        ".Xunit",
        ".XunitV3"
    ];

    public static async Task<(string newPackage, NuGetVersion version)?> TryResolve(
        string packageName,
        TestFramework framework,
        List<PackageSource> sources,
        SourceCacheContext cache)
    {
        // Check if this package has a framework-specific suffix
        var matchedSuffix = frameworkSuffixes
            .FirstOrDefault(suffix => packageName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        if (matchedSuffix == null)
        {
            return null;
        }

        var baseName = packageName[..^matchedSuffix.Length];
        var tunitCandidate = $"{baseName}.TUnit";

        var version = await NuGetPackageChecker.GetLatestStableVersion(tunitCandidate, sources, cache);
        if (version != null)
        {
            return (tunitCandidate, version);
        }

        // For XunitV3, also try .Xunit suffix as fallback
        if (framework == TestFramework.XunitV3 &&
            string.Equals(matchedSuffix, ".XunitV3", StringComparison.OrdinalIgnoreCase))
        {
            var xunitCandidate = $"{baseName}.Xunit";
            var xunitTunitCandidate = $"{baseName}.TUnit";
            // Already tried .TUnit above, so try checking if .Xunit has a .TUnit equivalent
            // Actually the .TUnit was already tried. For XunitV3, the fallback is that
            // the .Xunit suffix package might have a .TUnit equivalent already covered.
            // This case is already handled above.
        }

        return null;
    }
}
