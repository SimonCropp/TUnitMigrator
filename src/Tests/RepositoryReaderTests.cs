using NuGet.Configuration;

public class RepositoryReaderTests
{
    [Test]
    public async Task ReturnsRepository()
    {
        var source = new PackageSource("https://api.nuget.org/v3/index.json");

        var (repository, _) = await RepositoryReader.Read(source);

        await Assert.That(repository).IsNotNull();
        await Assert.That(repository.PackageSource.Source).IsEqualTo("https://api.nuget.org/v3/index.json");
    }

    [Test]
    public async Task ReturnsMetadataResource()
    {
        var source = new PackageSource("https://api.nuget.org/v3/index.json");

        var (_, metadataResource) = await RepositoryReader.Read(source);

        await Assert.That(metadataResource).IsNotNull();
    }

    [Test]
    public async Task CachesResults()
    {
        var source = new PackageSource("https://api.nuget.org/v3/index.json");

        var first = await RepositoryReader.Read(source);
        var second = await RepositoryReader.Read(source);

        await Assert.That(second.repository).IsSameReferenceAs(first.repository);
        await Assert.That(second.metadataResource).IsSameReferenceAs(first.metadataResource);
    }
}
