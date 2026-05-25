using System.Globalization;
using System.Text.Json;

namespace HealthMate.Sina.Tools;

internal static class ToolJson
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static JsonElement ToJsonElement<T>(T value) => JsonSerializer.SerializeToElement(value, JsonOptions);

    public static JsonElement ObjectSchema(params (string Name, string Type, string Description, bool Required)[] properties)
    {
        var props = properties.ToDictionary(
            p => p.Name,
            p => new { type = p.Type, description = p.Description },
            StringComparer.Ordinal);
        var required = properties.Where(p => p.Required).Select(p => p.Name).ToArray();
        return ToJsonElement(new { type = "object", properties = props, required });
    }

    public static int GetInt(JsonElement args, string name, int fallback = 0)
    {
        if (!args.TryGetProperty(name, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) => number,
            _ => fallback
        };
    }

    public static string? GetString(JsonElement args, string name)
    {
        return args.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    public static DateTime? GetDate(JsonElement args, string name)
    {
        var raw = GetString(args, name);
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date
            : null;
    }
}
