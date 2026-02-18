static class NoWarnScrubber
{
    public static bool ScrubXunitNoWarns(XDocument xml)
    {
        var updated = false;
        foreach (var noWarn in xml.Descendants("NoWarn").ToList())
        {
            var value = noWarn.Value;
            var parts = value.Split(';');
            var filtered = parts
                .Where(_ => !_.Trim().StartsWith("xUnit", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (filtered.Length == parts.Length)
            {
                continue;
            }

            var newValue = string.Join(';', filtered);
            if (string.IsNullOrWhiteSpace(newValue))
            {
                noWarn.Remove();
            }
            else
            {
                noWarn.Value = newValue;
            }

            updated = true;
        }

        return updated;
    }
}
