using System.Text;

static class XmlHelper
{
    public static (string newLine, bool hasTrailingNewline) DetectNewLineInfo(string path)
    {
        var content = File.ReadAllText(path);
        var newLine = content.Contains("\r\n") ? "\r\n" : "\n";
        var hasTrailingNewline = content.EndsWith(newLine);
        return (newLine, hasTrailingNewline);
    }

    public static async Task Save(XDocument xml, string path, string newLine, bool hasTrailingNewline)
    {
        var xmlSettings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            IndentChars = "  ",
            NewLineChars = newLine,
            Async = true
        };

        await using (var writer = XmlWriter.Create(path, xmlSettings))
        {
            await xml.SaveAsync(writer, Cancel.None);
        }

        if (hasTrailingNewline)
        {
            await File.AppendAllTextAsync(path, newLine);
        }
    }
}
