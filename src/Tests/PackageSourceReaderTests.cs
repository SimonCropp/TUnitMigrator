public class PackageSourceReaderTests
{
    [Test]
    public async Task ReadsDefaultNuGetSource()
    {
        var sources = PackageSourceReader.Read(Environment.CurrentDirectory);

        await Assert.That(sources).IsNotEmpty();
        await Assert.That(sources.Select(_ => _.Source)).Contains("https://api.nuget.org/v3/index.json");
    }

    [Test]
    public async Task AllSourcesAreEnabled()
    {
        var sources = PackageSourceReader.Read(Environment.CurrentDirectory);

        await Assert.That(sources.All(_ => _.IsEnabled)).IsTrue();
    }

    [Test]
    public async Task ReadsSourcesFromDirectoryWithNuGetConfig()
    {
        using var tempDir = new TempDirectory();
        var configPath = Path.Combine(tempDir, "nuget.config");
        await File.WriteAllTextAsync(configPath, """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """);

        var sources = PackageSourceReader.Read(tempDir);

        await Assert.That(sources).Count().IsEqualTo(1);
        await Assert.That(sources[0].Source).IsEqualTo("https://api.nuget.org/v3/index.json");
    }

    [Test]
    public async Task ReturnsEmptyWhenAllSourcesCleared()
    {
        using var tempDir = new TempDirectory();
        var configPath = Path.Combine(tempDir, "nuget.config");
        await File.WriteAllTextAsync(configPath, """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
              </packageSources>
            </configuration>
            """);

        var sources = PackageSourceReader.Read(tempDir);

        await Assert.That(sources).IsEmpty();
    }

    [Test]
    public async Task ExcludesDisabledSources()
    {
        using var tempDir = new TempDirectory();
        var configPath = Path.Combine(tempDir, "nuget.config");
        await File.WriteAllTextAsync(configPath, """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                <add key="disabled" value="https://disabled.example.com/v3/index.json" />
              </packageSources>
              <disabledPackageSources>
                <add key="disabled" value="true" />
              </disabledPackageSources>
            </configuration>
            """);

        var sources = PackageSourceReader.Read(tempDir);

        await Assert.That(sources).Count().IsEqualTo(1);
        await Assert.That(sources[0].Name).IsEqualTo("nuget.org");
    }
}
