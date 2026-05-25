using FluentAssertions;
using HealthMate.Sina.Tools;

namespace HealthMate.Tests.Sina.Tools;

public sealed class LabRangeParserTests
{
    [Theory]
    [InlineData("12-16", 12.0, 16.0)]
    [InlineData("12.5 - 16.5", 12.5, 16.5)]
    [InlineData("12 to 16", 12.0, 16.0)]
    [InlineData("<5", null, 5.0)]
    [InlineData("<= 5", null, 5.0)]
    [InlineData(">7", 7.0, null)]
    [InlineData(">= 7", 7.0, null)]
    public void TryParse_handles_supported_shapes(string raw, double? expectedLower, double? expectedUpper)
    {
        var parsed = LabRangeParser.TryParse(raw, out var lower, out var upper);

        parsed.Should().BeTrue();
        lower.Should().Be(expectedLower.HasValue ? (decimal)expectedLower.Value : null);
        upper.Should().Be(expectedUpper.HasValue ? (decimal)expectedUpper.Value : null);
    }

    [Theory]
    [InlineData("not provided")]
    [InlineData("")]
    public void TryParse_rejects_malformed_ranges(string raw)
    {
        LabRangeParser.TryParse(raw, out _, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(10, "12-16", "low")]
    [InlineData(18, "12-16", "high")]
    [InlineData(14, "12-16", "normal")]
    [InlineData(14, "unknown", "unknown")]
    public void GetAbnormality_returns_expected_flag(decimal value, string range, string expected)
    {
        LabRangeParser.GetAbnormality(value, range).Should().Be(expected);
    }
}
