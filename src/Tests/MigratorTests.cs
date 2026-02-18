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
}
