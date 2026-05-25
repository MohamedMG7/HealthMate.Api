using System.Globalization;
using System.Text.RegularExpressions;

namespace HealthMate.Sina.Tools;

public static partial class LabRangeParser
{
    public static bool TryParse(string? raw, out decimal? lower, out decimal? upper)
    {
        lower = null;
        upper = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.Trim().Replace("–", "-", StringComparison.Ordinal).Replace("to", "-", StringComparison.OrdinalIgnoreCase);

        var range = RangeRegex().Match(text);
        if (range.Success
            && TryParseDecimal(range.Groups["low"].Value, out var low)
            && TryParseDecimal(range.Groups["high"].Value, out var high))
        {
            lower = low;
            upper = high;
            return true;
        }

        var comparison = ComparisonRegex().Match(text);
        if (comparison.Success && TryParseDecimal(comparison.Groups["value"].Value, out var value))
        {
            var op = comparison.Groups["op"].Value;
            if (op.StartsWith('<'))
            {
                upper = value;
            }
            else
            {
                lower = value;
            }

            return true;
        }

        return false;
    }

    public static string GetAbnormality(decimal value, string? normalRange)
    {
        if (!TryParse(normalRange, out var lower, out var upper))
        {
            return "unknown";
        }

        if (lower.HasValue && value < lower.Value)
        {
            return "low";
        }

        if (upper.HasValue && value > upper.Value)
        {
            return "high";
        }

        return "normal";
    }

    public static bool IsSevere(decimal value, string? normalRange)
    {
        if (!TryParse(normalRange, out var lower, out var upper))
        {
            return false;
        }

        if (lower.HasValue && lower.Value > 0 && value < lower.Value / 2m)
        {
            return true;
        }

        return upper.HasValue && value > upper.Value * 2m;
    }

    private static bool TryParseDecimal(string raw, out decimal value)
    {
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    [GeneratedRegex(@"(?<low>\d+(?:\.\d+)?)\s*-\s*(?<high>\d+(?:\.\d+)?)")]
    private static partial Regex RangeRegex();

    [GeneratedRegex(@"^(?<op><=|>=|<|>)\s*(?<value>\d+(?:\.\d+)?)")]
    private static partial Regex ComparisonRegex();
}
