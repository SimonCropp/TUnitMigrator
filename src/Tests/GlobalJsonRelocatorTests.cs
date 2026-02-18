using System.Text.Json.Nodes;

public class GlobalJsonRelocatorTests
{
    [Test]
    public async Task RelocatesFromSubdirectoryToRoot()
    {
        using var tempDir = new TempDirectory();
        var subDir = Path.Combine(tempDir, "src");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "global.json"),
            """
            {
              "sdk": {
                "version": "10.0.103"
              }
            }
            """);

        await GlobalJsonRelocator.Relocate(tempDir);

        await Assert.That(File.Exists(Path.Combine(tempDir, "global.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(subDir, "global.json"))).IsFalse();
    }

    [Test]
    public async Task LeavesRootGlobalJsonInPlace()
    {
        using var tempDir = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(tempDir, "global.json"),
            """
            {
              "sdk": {
                "version": "10.0.103"
              }
            }
            """);

        await GlobalJsonRelocator.Relocate(tempDir);

        await Assert.That(File.Exists(Path.Combine(tempDir, "global.json"))).IsTrue();
    }

    [Test]
    public async Task SkipsWhenMultipleGlobalJsonFiles()
    {
        using var tempDir = new TempDirectory();
        var sub1 = Path.Combine(tempDir, "a");
        var sub2 = Path.Combine(tempDir, "b");
        Directory.CreateDirectory(sub1);
        Directory.CreateDirectory(sub2);
        await File.WriteAllTextAsync(Path.Combine(sub1, "global.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(sub2, "global.json"), "{}");

        await GlobalJsonRelocator.Relocate(tempDir);

        // Neither relocated — no root global.json created
        await Assert.That(File.Exists(Path.Combine(tempDir, "global.json"))).IsFalse();
    }

    [Test]
    public async Task SkipsWhenNoGlobalJson()
    {
        using var tempDir = new TempDirectory();

        await GlobalJsonRelocator.Relocate(tempDir);

        await Assert.That(File.Exists(Path.Combine(tempDir, "global.json"))).IsFalse();
    }

    [Test]
    public async Task AddsTestRunnerNode()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "global.json");
        await File.WriteAllTextAsync(path,
            """
            {
              "sdk": {
                "version": "10.0.103"
              }
            }
            """);

        await GlobalJsonRelocator.Relocate(tempDir);

        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        await Assert.That(json["test"]?["runner"]?.ToString()).IsEqualTo("Microsoft.Testing.Platform");
    }

    [Test]
    public async Task PreservesExistingTestRunner()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "global.json");
        await File.WriteAllTextAsync(path,
            """
            {
              "sdk": {
                "version": "10.0.103"
              },
              "test": {
                "runner": "Microsoft.Testing.Platform"
              }
            }
            """);

        await GlobalJsonRelocator.Relocate(tempDir);

        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        await Assert.That(json["test"]?["runner"]?.ToString()).IsEqualTo("Microsoft.Testing.Platform");
    }

    [Test]
    public async Task AddsRunnerToExistingTestNode()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "global.json");
        await File.WriteAllTextAsync(path,
            """
            {
              "sdk": {
                "version": "10.0.103"
              },
              "test": {
                "other": "value"
              }
            }
            """);

        await GlobalJsonRelocator.Relocate(tempDir);

        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        await Assert.That(json["test"]?["runner"]?.ToString()).IsEqualTo("Microsoft.Testing.Platform");
        await Assert.That(json["test"]?["other"]?.ToString()).IsEqualTo("value");
    }

    [Test]
    public async Task PreservesSdkNode()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "global.json");
        await File.WriteAllTextAsync(path,
            """
            {
              "sdk": {
                "version": "10.0.103"
              }
            }
            """);

        await GlobalJsonRelocator.Relocate(tempDir);

        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        await Assert.That(json["sdk"]?["version"]?.ToString()).IsEqualTo("10.0.103");
    }

    [Test]
    public async Task RelocationPreservesContent()
    {
        using var tempDir = new TempDirectory();
        var subDir = Path.Combine(tempDir, "src");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "global.json"),
            """
            {
              "sdk": {
                "version": "9.0.100",
                "rollForward": "latestFeature"
              }
            }
            """);

        await GlobalJsonRelocator.Relocate(tempDir);

        var json = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(tempDir, "global.json")))!;
        await Assert.That(json["sdk"]?["version"]?.ToString()).IsEqualTo("9.0.100");
        await Assert.That(json["sdk"]?["rollForward"]?.ToString()).IsEqualTo("latestFeature");
    }

    [Test]
    public async Task RelocatesFromDeeplyNestedDirectory()
    {
        using var tempDir = new TempDirectory();
        var deepDir = Path.Combine(tempDir, "a", "b", "c");
        Directory.CreateDirectory(deepDir);
        await File.WriteAllTextAsync(Path.Combine(deepDir, "global.json"), """{ "sdk": { "version": "10.0.103" } }""");

        await GlobalJsonRelocator.Relocate(tempDir);

        await Assert.That(File.Exists(Path.Combine(tempDir, "global.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(deepDir, "global.json"))).IsFalse();
    }

    [Test]
    public async Task PreservesTrailingNewline()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "global.json");
        await File.WriteAllTextAsync(path, "{\n  \"sdk\": {\n    \"version\": \"10.0.103\"\n  }\n}\n");

        await GlobalJsonRelocator.Relocate(tempDir);

        var content = await File.ReadAllTextAsync(path);
        await Assert.That(content).EndsWith("\n");
    }

    [Test]
    public async Task DoesNotModifyFileWhenRunnerAlreadySet()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "global.json");
        var original = """
            {
              "sdk": {
                "version": "10.0.103"
              },
              "test": {
                "runner": "Microsoft.Testing.Platform"
              }
            }
            """;
        await File.WriteAllTextAsync(path, original);
        var lastWrite = File.GetLastWriteTimeUtc(path);

        await Task.Delay(50);
        await GlobalJsonRelocator.Relocate(tempDir);

        await Assert.That(File.GetLastWriteTimeUtc(path)).IsEqualTo(lastWrite);
    }
}
