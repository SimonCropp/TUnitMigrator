public class MigratorTests
{
    static string ScenariosDir => Path.Combine(ProjectFiles.SolutionDirectory, "Scenarios");

    static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        foreach (var dir in Directory.EnumerateDirectories(source))
        {
            var dirName = Path.GetFileName(dir);
            CopyDirectory(dir, Path.Combine(destination, dirName));
        }
    }

    [Test]
    [Arguments("MSTest")]
    [Arguments("NUnit")]
    [Arguments("Xunit")]
    [Arguments("XunitV3")]
    public async Task Migration(string framework)
    {
        using var tempDir = new TempDirectory();
        var scenarioName = $"{framework}Scenario";
        var source = Path.GetFullPath(Path.Combine(ScenariosDir, scenarioName));
        CopyDirectory(source, tempDir);
        // Create .git marker directory (git cannot track .git directories in scenarios)
        Directory.CreateDirectory(Path.Combine(tempDir, ".git"));
        await Migrator.Migrate(tempDir);

        var props = await File.ReadAllTextAsync(Path.Combine(tempDir, "Directory.Packages.props"));
        var csproj = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "TestProject.csproj"));
        var yml = await File.ReadAllTextAsync(Path.Combine(tempDir, ".github", "workflows", "ci.yml"));
        var globalJson = await File.ReadAllTextAsync(Path.Combine(tempDir, "global.json"));

        await Verify(
            new
            {
                props,
                csproj,
                yml,
                globalJson
            });
    }

    [Test]
    public async Task AlreadyMigratedToTUnitIsUnchanged()
    {
        using var tempDir = new TempDirectory();
        var source = Path.GetFullPath(Path.Combine(ScenariosDir, "TUnitScenario"));
        CopyDirectory(source, tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, ".git"));

        var propsBefore = await File.ReadAllTextAsync(Path.Combine(tempDir, "Directory.Packages.props"));
        var csprojBefore = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "TestProject.csproj"));
        var ymlBefore = await File.ReadAllTextAsync(Path.Combine(tempDir, ".github", "workflows", "ci.yml"));
        var globalJsonBefore = await File.ReadAllTextAsync(Path.Combine(tempDir, "global.json"));

        await Migrator.Migrate(tempDir);

        var propsAfter = await File.ReadAllTextAsync(Path.Combine(tempDir, "Directory.Packages.props"));
        var csprojAfter = await File.ReadAllTextAsync(Path.Combine(tempDir, "src", "TestProject.csproj"));
        var ymlAfter = await File.ReadAllTextAsync(Path.Combine(tempDir, ".github", "workflows", "ci.yml"));
        var globalJsonAfter = await File.ReadAllTextAsync(Path.Combine(tempDir, "global.json"));

        await Assert.That(propsAfter).IsEqualTo(propsBefore);
        await Assert.That(csprojAfter).IsEqualTo(csprojBefore);
        await Assert.That(ymlAfter).IsEqualTo(ymlBefore);
        await Assert.That(globalJsonAfter).IsEqualTo(globalJsonBefore);
    }
}
