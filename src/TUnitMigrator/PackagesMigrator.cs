static class PackagesMigrator
{
    // Coverlet is unnecessary because Microsoft.Testing.Platform (used by TUnit)
    // has built-in code coverage support via --collect-code-coverage / --coverage.
    // Microsoft.NET.Test.Sdk is the VSTest runner glue and is replaced by Microsoft.Testing.Platform.
    static readonly HashSet<string> alwaysRemove = new(StringComparer.OrdinalIgnoreCase)
    {
        "coverlet.collector",
        "coverlet.msbuild",
        "Microsoft.NET.Test.Sdk",
        "Microsoft.CodeCoverage"
    };

    static readonly List<string> removePrefixes =
    [
        "MSTest",
        "Microsoft.Testing.",
        "Microsoft.TestPlatform.",
        "NUnit",
        "xunit"
    ];

    public static async Task<List<(string OldPackage, string NewPackage)>> Migrate(
        string propsPath,
        NuGetVersion tunitVersion,
        List<PackageSource> sources,
        SourceCacheContext cache)
    {
        var (newLine, hasTrailingNewline) = XmlHelper.DetectNewLineInfo(propsPath);
        var xml = XDocument.Load(propsPath);
        var migrations = new List<(string OldPackage, string NewPackage)>();

        var packageVersions = xml.Descendants("PackageVersion").ToList();

        // Remove framework-specific and always-remove packages
        foreach (var element in packageVersions.ToList())
        {
            var name = element.Attribute("Include")?.Value;
            if (name == null)
            {
                continue;
            }

            if (alwaysRemove.Contains(name) ||
                removePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
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

        // Handle PackageReference entries in conditional ItemGroups (e.g. test project conditions)
        var packageReferences = xml.Descendants("PackageReference").ToList();
        var addTUnitRef = false;
        foreach (var element in packageReferences)
        {
            var name = element.Attribute("Include")?.Value;
            if (name == null)
            {
                continue;
            }

            if (alwaysRemove.Contains(name) ||
                removePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                Log.Information("Removing PackageReference {Package} from Directory.Packages.props", name);
                addTUnitRef = true;
                element.Remove();
            }
        }

        // Rename extension PackageReferences
        packageReferences = xml.Descendants("PackageReference").ToList();
        foreach (var element in packageReferences)
        {
            var name = element.Attribute("Include")?.Value;
            if (name == null)
            {
                continue;
            }

            var migration = migrations.FirstOrDefault(_ => string.Equals(_.OldPackage, name, StringComparison.OrdinalIgnoreCase));
            if (migration != default && !string.IsNullOrEmpty(migration.NewPackage))
            {
                Log.Information("Updating PackageReference {Old} -> {New} in Directory.Packages.props", name, migration.NewPackage);
                element.SetAttributeValue("Include", migration.NewPackage);
            }
        }

        // Add TUnit PackageReference if any were removed and TUnit ref doesn't already exist
        if (addTUnitRef)
        {
            var hasTUnitRef = xml.Descendants("PackageReference")
                .Any(_ => string.Equals(_.Attribute("Include")?.Value, "TUnit", StringComparison.OrdinalIgnoreCase));

            if (!hasTUnitRef)
            {
                // Find first ItemGroup that had PackageReference entries (conditional block)
                var refItemGroup = xml.Descendants("ItemGroup")
                    .FirstOrDefault(_ => _.Descendants("PackageReference").Any())
                    ?? xml.Descendants("ItemGroup").FirstOrDefault();
                if (refItemGroup != null)
                {
                    refItemGroup.Add(new XElement("PackageReference", new XAttribute("Include", "TUnit")));
                    Log.Information("Added PackageReference TUnit to Directory.Packages.props");
                }
            }
        }

        NoWarnScrubber.ScrubXunitNoWarns(xml);

        await XmlHelper.Save(xml, propsPath, newLine, hasTrailingNewline);

        return migrations;
    }
}
