public class XmlHelperTests
{
    [Test]
    public async Task DetectsLfNewLine()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "test.xml");
        await File.WriteAllTextAsync(path, "<Project>\n  <ItemGroup />\n</Project>\n");

        var (newLine, _) = XmlHelper.DetectNewLineInfo(path);

        await Assert.That(newLine).IsEqualTo("\n");
    }

    [Test]
    public async Task DetectsCrLfNewLine()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "test.xml");
        await File.WriteAllTextAsync(path, "<Project>\r\n  <ItemGroup />\r\n</Project>\r\n");

        var (newLine, _) = XmlHelper.DetectNewLineInfo(path);

        await Assert.That(newLine).IsEqualTo("\r\n");
    }

    [Test]
    public async Task DetectsTrailingNewline()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "test.xml");
        await File.WriteAllTextAsync(path, "<Project />\n");

        var (_, hasTrailingNewline) = XmlHelper.DetectNewLineInfo(path);

        await Assert.That(hasTrailingNewline).IsTrue();
    }

    [Test]
    public async Task DetectsNoTrailingNewline()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "test.xml");
        await File.WriteAllTextAsync(path, "<Project />");

        var (_, hasTrailingNewline) = XmlHelper.DetectNewLineInfo(path);

        await Assert.That(hasTrailingNewline).IsFalse();
    }

    [Test]
    public async Task SavePreservesLfNewLines()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "test.xml");
        var xml = new XDocument(new XElement("Project", new XElement("ItemGroup")));

        await XmlHelper.Save(xml, path, "\n", hasTrailingNewline: false);

        var content = await File.ReadAllTextAsync(path);
        await Assert.That(content).DoesNotContain("\r\n");
        await Assert.That(content).Contains("\n");
    }

    [Test]
    public async Task SavePreservesCrLfNewLines()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "test.xml");
        var xml = new XDocument(new XElement("Project", new XElement("ItemGroup")));

        await XmlHelper.Save(xml, path, "\r\n", hasTrailingNewline: false);

        var content = await File.ReadAllTextAsync(path);
        await Assert.That(content).Contains("\r\n");
    }

    [Test]
    public async Task SaveAddsTrailingNewline()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "test.xml");
        var xml = new XDocument(new XElement("Project"));

        await XmlHelper.Save(xml, path, "\n", hasTrailingNewline: true);

        var content = await File.ReadAllTextAsync(path);
        await Assert.That(content).EndsWith("\n");
    }

    [Test]
    public async Task SaveOmitsTrailingNewline()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "test.xml");
        var xml = new XDocument(new XElement("Project"));

        await XmlHelper.Save(xml, path, "\n", hasTrailingNewline: false);

        var content = await File.ReadAllTextAsync(path);
        await Assert.That(content).EndsWith("/>");
    }

    [Test]
    public async Task SaveOmitsXmlDeclaration()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "test.xml");
        var xml = new XDocument(new XElement("Project"));

        await XmlHelper.Save(xml, path, "\n", hasTrailingNewline: false);

        var content = await File.ReadAllTextAsync(path);
        await Assert.That(content).DoesNotContain("<?xml");
    }

    [Test]
    public async Task SaveRoundTripsWithDetect()
    {
        using var tempDir = new TempDirectory();
        var path = Path.Combine(tempDir, "test.xml");
        var original = "<Project>\n  <ItemGroup />\n</Project>\n";
        await File.WriteAllTextAsync(path, original);

        var (newLine, hasTrailingNewline) = XmlHelper.DetectNewLineInfo(path);
        var xml = XDocument.Load(path);
        await XmlHelper.Save(xml, path, newLine, hasTrailingNewline);

        var result = await File.ReadAllTextAsync(path);
        await Assert.That(result).IsEqualTo(original);
    }
}
