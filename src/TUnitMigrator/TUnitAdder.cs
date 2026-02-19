static class TUnitAdder
{
    public static Task AddToProps(string propsPath, NuGetVersion tunitVersion)
    {
        var (newLine, hasTrailingNewline) = XmlHelper.DetectNewLineInfo(propsPath);
        var xml = XDocument.Load(propsPath);

        var existingTUnit = xml.Descendants("PackageVersion")
            .FirstOrDefault(_ => string.Equals(_.Attribute("Include")?.Value, "TUnit", StringComparison.OrdinalIgnoreCase));

        if (existingTUnit != null)
        {
            return Task.CompletedTask;
        }

        var itemGroup = xml.Descendants("ItemGroup").FirstOrDefault();
        if (itemGroup == null)
        {
            return Task.CompletedTask;
        }

        itemGroup.Add(new XElement(
            "PackageVersion",
            new XAttribute("Include", "TUnit"),
            new XAttribute("Version", tunitVersion.ToString())));
        Log.Information("Added TUnit {Version} to Directory.Packages.props", tunitVersion);

        return XmlHelper.Save(xml, propsPath, newLine, hasTrailingNewline);
    }

    static readonly List<string> testFrameworkPrefixes = ["MSTest", "Microsoft.Testing.", "NUnit", "xunit"];

    public static async Task AddToCsprojs(string directory)
    {
        var csprojFiles = FileSystem.EnumerateFiles(directory, "*.csproj");

        foreach (var csprojPath in csprojFiles)
        {
            var csprojXml = XDocument.Load(csprojPath);

            // Only add to csprojs that reference the old test framework
            var refsOldFramework = csprojXml.Descendants("PackageReference")
                .Any(_ => testFrameworkPrefixes.Any(prefix => (_.Attribute("Include")?.Value ?? "").StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
            if (!refsOldFramework)
            {
                continue;
            }

            var hasTUnit = csprojXml.Descendants("PackageReference")
                .Any(_ => string.Equals(_.Attribute("Include")?.Value, "TUnit", StringComparison.OrdinalIgnoreCase));

            if (hasTUnit)
            {
                continue;
            }

            var (newLine, hasTrailingNewline) = XmlHelper.DetectNewLineInfo(csprojPath);
            var itemGroup = csprojXml.Descendants("ItemGroup")
                .FirstOrDefault(_ => _.Descendants("PackageReference").Any());

            if (itemGroup == null)
            {
                continue;
            }

            itemGroup.Add(new XElement("PackageReference", new XAttribute("Include", "TUnit")));
            Log.Information("Added PackageReference TUnit to {File}", Path.GetFileName(csprojPath));
            await XmlHelper.Save(csprojXml, csprojPath, newLine, hasTrailingNewline);
        }
    }
}
