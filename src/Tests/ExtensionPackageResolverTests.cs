using NuGet.Configuration;

public class ExtensionPackageResolverTests
{
    static List<PackageSource> sources = null!;
    static SourceCacheContext cache = null!;

    [Before(Class)]
    public static void Setup()
    {
        sources = PackageSourceReader.Read(Environment.CurrentDirectory);
        cache = new SourceCacheContext { RefreshMemoryCache = true };
    }

    [After(Class)]
    public static void Cleanup() => cache.Dispose();

    [Test]
    public async Task ResolvesVerifyMSTest()
    {
        var result = await ExtensionPackageResolver.TryResolve("Verify.MSTest", sources, cache);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.newPackage).IsEqualTo("Verify.TUnit");
    }

    [Test]
    public async Task ResolvesVerifyNUnit()
    {
        var result = await ExtensionPackageResolver.TryResolve("Verify.NUnit", sources, cache);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.newPackage).IsEqualTo("Verify.TUnit");
    }

    [Test]
    public async Task ResolvesVerifyXunit()
    {
        var result = await ExtensionPackageResolver.TryResolve("Verify.Xunit", sources, cache);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.newPackage).IsEqualTo("Verify.TUnit");
    }

    [Test]
    public async Task ResolvesVerifyXunitV3()
    {
        var result = await ExtensionPackageResolver.TryResolve("Verify.XunitV3", sources, cache);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.newPackage).IsEqualTo("Verify.TUnit");
    }

    [Test]
    public async Task ReturnsNullForNoSuffix()
    {
        var result = await ExtensionPackageResolver.TryResolve("Newtonsoft.Json", sources, cache);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ReturnsNullWhenTUnitVariantDoesNotExist()
    {
        var result = await ExtensionPackageResolver.TryResolve("NonExistentPackage12345.Xunit", sources, cache);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ReturnsVersion()
    {
        var result = await ExtensionPackageResolver.TryResolve("Verify.MSTest", sources, cache);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.version).IsNotNull();
    }
}
