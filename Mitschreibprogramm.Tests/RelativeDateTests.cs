using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Tests;

public sealed class RelativeDateTests
{
    private static readonly DateTime Now = new(2026, 9, 29, 10, 30, 0);

    [Theory]
    [InlineData("2026-09-29 10:04", "heute 10:04")]
    [InlineData("2026-09-29 00:00", "heute 00:00")]
    [InlineData("2026-09-28 23:59", "gestern")]
    [InlineData("2026-09-28 00:01", "gestern")]
    [InlineData("2026-09-27 12:00", "27.09.")]
    [InlineData("2026-01-03 08:00", "03.01.")]
    [InlineData("2025-12-31 08:00", "31.12.2025")]
    public void Format_IsShortAndRelativeToToday(string value, string expected) =>
        Assert.Equal(expected, RelativeDate.Format(DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture), Now));

    [Fact]
    public void Format_CountsYesterdayAcrossTheTurnOfTheYear() =>
        Assert.Equal("gestern", RelativeDate.Format(new DateTime(2026, 12, 31, 18, 0, 0), new DateTime(2027, 1, 1, 9, 0, 0)));
}
