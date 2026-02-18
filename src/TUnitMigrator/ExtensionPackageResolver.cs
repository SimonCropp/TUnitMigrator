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

        return null;
    }
}
