static class CsprojMigrator
{
    public static async Task Migrate(string directory, List<(string OldPackage, string NewPackage)> migrations)
    {
        var csprojFiles = FileSystem.EnumerateFiles(directory, "*.csproj");

        foreach (var csprojPath in csprojFiles)
        {
            var updated = false;
            var (newLine, hasTrailingNewline) = XmlHelper.DetectNewLineInfo(csprojPath);
            var csprojXml = XDocument.Load(csprojPath);
            var addTUnit = false;

            // Remove references for removed packages (NewPackage is empty)
            // Rename references for migrated extension packages
            foreach (var (oldPackage, newPackage) in migrations)
            {
                var packageReferences = csprojXml.Descendants("PackageReference")
                    .Where(_ => string.Equals(
                        _.Attribute("Include")?.Value,
                        oldPackage,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var packageRef in packageReferences)
                {
                    if (string.IsNullOrEmpty(newPackage))
                    {
                        packageRef.Remove();
                        updated = true;
                        addTUnit = true;
                        Log.Information(
                            "Removed PackageReference {Package} from {File}",
                            oldPackage,
                            Path.GetFileName(csprojPath));
                    }
                    else
                    {
                        packageRef.SetAttributeValue("Include", newPackage);
                        updated = true;
                        Log.Information(
                            "Updated PackageReference {OldPackage} -> {NewPackage} in {File}",
                            oldPackage,
                            newPackage,
                            Path.GetFileName(csprojPath));
                    }
                }
            }

            // Add TUnit reference if any references were modified and TUnit isn't already there
            if (addTUnit)
            {
                var hasTUnit = csprojXml.Descendants("PackageReference")
                    .Any(_ => string.Equals(_.Attribute("Include")?.Value, "TUnit", StringComparison.OrdinalIgnoreCase));

                if (!hasTUnit)
                {
                    var itemGroup = csprojXml.Descendants("ItemGroup")
                        .FirstOrDefault(_ => _.Descendants("PackageReference").Any())
                        ?? csprojXml.Descendants("ItemGroup").FirstOrDefault();
                    if (itemGroup != null)
                    {
                        itemGroup.Add(new XElement("PackageReference", new XAttribute("Include", "TUnit")));
                        updated = true;
                        Log.Information("Added PackageReference TUnit to {File}", Path.GetFileName(csprojPath));
                    }
                }
            }

            if (updated)
            {
                await XmlHelper.Save(csprojXml, csprojPath, newLine, hasTrailingNewline);
            }
        }
    }
}
