public class FormatterTests
{
    [Test]
    [Arguments(0, 0, 0, 500, "500ms")]
    [Arguments(0, 0, 0, 1, "1ms")]
    [Arguments(0, 0, 1, 500, "1.5s")]
    [Arguments(0, 0, 30, 0, "30.0s")]
    [Arguments(0, 1, 30, 0, "1m30s")]
    [Arguments(0, 5, 0, 0, "5m0s")]
    [Arguments(1, 0, 0, 0, "1h0m")]
    [Arguments(2, 30, 0, 0, "2h30m")]
    public async Task FormatElapsed(int hours, int minutes, int seconds, int ms, string expected)
    {
        var elapsed = new TimeSpan(0, hours, minutes, seconds, ms);
        await Assert.That(Formatter.FormatElapsed(elapsed)).IsEqualTo(expected);
    }
}
