static class NuGetPackageChecker
{
    static readonly ConcurrentDictionary<string, NuGetVersion?> versionCache = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<NuGetVersion?> GetLatestStableVersion(
        string packageId,
        List<PackageSource> sources,
        SourceCacheContext cache)
    {
        if (versionCache.TryGetValue(packageId, out var cached))
        {
            return cached;
        }

        foreach (var source in sources)
        {
            var (repository, _) = await RepositoryReader.Read(source);
            var findResource = await repository.GetResourceAsync<FindPackageByIdResource>();

            var versions = await findResource.GetAllVersionsAsync(
                packageId,
                cache,
                SerilogNuGetLogger.Instance,
                Cancel.None);

            var latest = versions
                .Where(_ => !_.IsPrerelease)
                .OrderByDescending(_ => _)
                .FirstOrDefault();

            if (latest != null)
            {
                versionCache[packageId] = latest;
                return latest;
            }
        }

        versionCache[packageId] = null;
        return null;
    }}
