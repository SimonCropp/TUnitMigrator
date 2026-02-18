public class FileSystemTests
{
    [Test]
    public async Task FindProjectRoots_DirectoryWithGit_ReturnsSelf()
    {
        using var tempDir = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(tempDir, ".git"));

        var roots = FileSystem.FindProjectRoots(tempDir);

        await Assert.That(roots).Count().IsEqualTo(1);
        await Assert.That(roots[0]).IsEqualTo(tempDir);
    }

    [Test]
    public async Task FindProjectRoots_SubdirectoriesWithGit_ReturnsThose()
    {
        using var tempDir = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(tempDir, "repoA", ".git"));
        Directory.CreateDirectory(Path.Combine(tempDir, "repoB", ".git"));
        Directory.CreateDirectory(Path.Combine(tempDir, "notARepo"));

        var roots = FileSystem.FindProjectRoots(tempDir);

        await Assert.That(roots).Count().IsEqualTo(2);
        await Assert.That(roots.Select(_ => Path.GetFileName(_))).IsEquivalentTo(["repoA", "repoB"]);
    }

    [Test]
    public async Task FindProjectRoots_NoGitAnywhere_ReturnsEmpty()
    {
        using var tempDir = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(tempDir, "someDir"));

        var roots = FileSystem.FindProjectRoots(tempDir);

        await Assert.That(roots).IsEmpty();
    }

    [Test]
    public async Task EnumerateFiles_FindsFilesRecursively()
    {
        using var tempDir = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(tempDir, "sub"));
        await File.WriteAllTextAsync(Path.Combine(tempDir, "a.txt"), "");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "b.cs"), "");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "sub", "c.txt"), "");

        var txtFiles = FileSystem.EnumerateFiles(tempDir, "*.txt").ToList();

        await Assert.That(txtFiles).Count().IsEqualTo(2);
        await Assert.That(txtFiles.Select(_ => Path.GetFileName(_))).IsEquivalentTo(["a.txt", "c.txt"]);
    }

    [Test]
    public async Task EnumerateFiles_NoMatches_ReturnsEmpty()
    {
        using var tempDir = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(tempDir, "a.cs"), "");

        var result = FileSystem.EnumerateFiles(tempDir, "*.txt").ToList();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task FindSolutionFile_PrefersSlnxOverSln()
    {
        using var tempDir = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(tempDir, "App.sln"), "");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "App.slnx"), "");

        var result = FileSystem.FindSolutionFile(tempDir);

        await Assert.That(Path.GetFileName(result!)).IsEqualTo("App.slnx");
    }

    [Test]
    public async Task FindSolutionFile_FallsBackToSln()
    {
        using var tempDir = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(tempDir, "App.sln"), "");

        var result = FileSystem.FindSolutionFile(tempDir);

        await Assert.That(Path.GetFileName(result!)).IsEqualTo("App.sln");
    }

    [Test]
    public async Task FindSolutionFile_NoSolution_ReturnsNull()
    {
        using var tempDir = new TempDirectory();

        var result = FileSystem.FindSolutionFile(tempDir);

        await Assert.That(result).IsNull();
    }
}
