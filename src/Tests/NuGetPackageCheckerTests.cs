using NuGet.Configuration;

public class NuGetPackageCheckerTests
{
    static List<PackageSource> sources = null!;
    static SourceCacheContext cache = null!;

    [Before(Class)]
    public static void Setup()
    {
        sources = PackageSourceReader.Read(Environment.CurrentDirectory);
        cache = new()
        {
            RefreshMemoryCache = true
        };
    }

    [After(Class)]
    public static void Cleanup() => cache.Dispose();

    [Test]
    public async Task ReturnsVersionForKnownPackage()
    {
        var version = await NuGetPackageChecker.GetLatestStableVersion("TUnit", sources, cache);

        await Assert.That(version).IsNotNull();
    }

    [Test]
    public async Task ReturnsNullForNonExistentPackage()
    {
        var version = await NuGetPackageChecker.GetLatestStableVersion("NonExistentPackage12345XYZ", sources, cache);

        await Assert.That(version).IsNull();
    }

    [Test]
    public async Task ReturnedVersionIsNotPrerelease()
    {
        var version = await NuGetPackageChecker.GetLatestStableVersion("TUnit", sources, cache);

        await Assert.That(version).IsNotNull();
        await Assert.That(version!.IsPrerelease).IsFalse();
    }

    [Test]
    public async Task CachesResults()
    {
        var first = await NuGetPackageChecker.GetLatestStableVersion("Newtonsoft.Json", sources, cache);
        var second = await NuGetPackageChecker.GetLatestStableVersion("Newtonsoft.Json", sources, cache);

        await Assert.That(first).IsEqualTo(second);
    }
}
