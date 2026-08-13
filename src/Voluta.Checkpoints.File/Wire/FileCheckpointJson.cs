using System.Text.Json;

namespace Voluta.Checkpoints.File.Wire;

/// <summary>
///     JSON element conversion for file checkpoint wire values.
/// </summary>
internal static class FileCheckpointJson
{
    public static JsonElement? ToElement(object? value)
    {
        return value is null ? null : JsonSerializer.SerializeToElement(value);
    }

    public static object? FromElement(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var json = element.Value;
        return json.ValueKind switch
        {
            JsonValueKind.String => json.GetString(),
            JsonValueKind.Number when json.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when json.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => json.EnumerateArray()
                .Select(static item => FromElement(item))
                .ToList(),
            _ => json.GetRawText(),
        };
    }
}
