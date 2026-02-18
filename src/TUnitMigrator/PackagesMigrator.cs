static class PackagesMigrator
{
    // Coverlet is unnecessary because Microsoft.Testing.Platform (used by TUnit)
    // has built-in code coverage support via --collect-code-coverage / --coverage.
    // Microsoft.NET.Test.Sdk is the VSTest runner glue and is replaced by Microsoft.Testing.Platform.
    static readonly HashSet<string> alwaysRemove = new(StringComparer.OrdinalIgnoreCase)
    {
        "coverlet.collector",
        "coverlet.msbuild",
        "Microsoft.NET.Test.Sdk"
    };

    static HashSet<string> GetFrameworkPackagesToRemove(TestFramework framework) =>
        FrameworkDetector.GetPackageNames(framework);

    public static async Task<List<(string OldPackage, string NewPackage)>> Migrate(
        string propsPath,
        TestFramework framework,
        NuGetVersion tunitVersion,
        List<PackageSource> sources,
        SourceCacheContext cache)
    {
        var (newLine, hasTrailingNewline) = XmlHelper.DetectNewLineInfo(propsPath);
        var xml = XDocument.Load(propsPath);
        var migrations = new List<(string OldPackage, string NewPackage)>();
        var frameworkPackages = GetFrameworkPackagesToRemove(framework);
        var allRemove = new HashSet<string>(alwaysRemove, StringComparer.OrdinalIgnoreCase);
        foreach (var pkg in frameworkPackages)
        {
            allRemove.Add(pkg);
        }

        var packageVersions = xml.Descendants("PackageVersion").ToList();

        // Remove framework-specific and always-remove packages
        foreach (var element in packageVersions.ToList())
        {
            var name = element.Attribute("Include")?.Value;
            if (name == null)
            {
                continue;
            }

            if (allRemove.Contains(name))
            {
                Log.Information("Removing package {Package} from Directory.Packages.props", name);
                migrations.Add((name, ""));
                element.Remove();
            }
        }

        // Handle extension packages (suffix replacement)
        packageVersions = xml.Descendants("PackageVersion").ToList();
        foreach (var element in packageVersions)
        {
            var name = element.Attribute("Include")?.Value;
            if (name == null)
            {
                continue;
            }

            var resolved = await ExtensionPackageResolver.TryResolve(name, sources, cache);
            if (resolved != null)
            {
                var (newPackage, newVersion) = resolved.Value;
                Log.Information("Migrating extension package {Old} -> {New} ({Version})", name, newPackage, newVersion);
                element.SetAttributeValue("Include", newPackage);
                element.SetAttributeValue("Version", newVersion.ToString());
                migrations.Add((name, newPackage));
            }
        }

        // Add TUnit package
        var existingTUnit = xml.Descendants("PackageVersion")
            .FirstOrDefault(_ => string.Equals(_.Attribute("Include")?.Value, "TUnit", StringComparison.OrdinalIgnoreCase));

        if (existingTUnit == null)
        {
            var itemGroup = xml.Descendants("ItemGroup").FirstOrDefault();
            if (itemGroup != null)
            {
                var tunitElement = new XElement(
                    "PackageVersion",
                    new XAttribute("Include", "TUnit"),
                    new XAttribute("Version", tunitVersion.ToString()));
                itemGroup.Add(tunitElement);
                Log.Information("Added TUnit {Version} to Directory.Packages.props", tunitVersion);
            }
        }

        NoWarnScrubber.ScrubXunitNoWarns(xml);

        await XmlHelper.Save(xml, propsPath, newLine, hasTrailingNewline);

        return migrations;
    }
}
