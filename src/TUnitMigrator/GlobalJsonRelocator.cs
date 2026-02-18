static class GlobalJsonRelocator
{
    public static async Task Relocate(string projectRoot)
    {
        var globalJsonFiles = FileSystem.EnumerateFiles(projectRoot, "global.json").ToList();

        if (globalJsonFiles.Count == 0)
        {
            Log.Information("No global.json found in {Directory}", projectRoot);
            return;
        }

        if (globalJsonFiles.Count > 1)
        {
            Log.Warning("Multiple global.json files found in {Directory}, skipping relocation", projectRoot);
            return;
        }

        var globalJsonPath = globalJsonFiles[0];
        var rootGlobalJson = Path.Combine(projectRoot, "global.json");

        if (!string.Equals(Path.GetFullPath(globalJsonPath), Path.GetFullPath(rootGlobalJson), StringComparison.OrdinalIgnoreCase))
        {
            var content = await File.ReadAllTextAsync(globalJsonPath);
            await File.WriteAllTextAsync(rootGlobalJson, content);
            File.Delete(globalJsonPath);
            Log.Information("Relocated global.json from {Source} to {Destination}", globalJsonPath, rootGlobalJson);
        }
        else
        {
            Log.Information("global.json already at root of {Directory}", projectRoot);
        }

        await EnsureTestRunner(rootGlobalJson);
    }

    static async Task EnsureTestRunner(string globalJsonPath)
    {
        var content = await File.ReadAllTextAsync(globalJsonPath);
        var json = JsonNode.Parse(content, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });

        if (json is not JsonObject root)
        {
            Log.Warning("global.json is not a JSON object: {Path}", globalJsonPath);
            return;
        }

        var testNode = root["test"];
        if (testNode is JsonObject testObj)
        {
            if (testObj["runner"] != null)
            {
                Log.Information("global.json already has test runner configured");
                return;
            }

            testObj["runner"] = "Microsoft.Testing.Platform";
        }
        else
        {
            root["test"] = new JsonObject
            {
                ["runner"] = "Microsoft.Testing.Platform"
            };
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var newContent = root.ToJsonString(options);

        // Preserve trailing newline if original had one
        var newLine = content.Contains("\r\n") ? "\r\n" : "\n";
        if (content.EndsWith(newLine))
        {
            newContent += newLine;
        }

        await File.WriteAllTextAsync(globalJsonPath, newContent);
        Log.Information("Added test runner 'Microsoft.Testing.Platform' to global.json");
    }
}
