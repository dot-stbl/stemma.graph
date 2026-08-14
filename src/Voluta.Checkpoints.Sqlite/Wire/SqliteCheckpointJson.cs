using System.Collections;
using System.Text.Json;
using Voluta.Abstractions.Checkpoint;

namespace Voluta.Checkpoints.Sqlite.Wire;

/// <summary>
///     JSON element conversion for SQLite checkpoint wire values (format v1 allow-list).
/// </summary>
/// <remarks>
///     Allowed shapes (wire format v1): <c>null</c>, string, bool, char, numeric primitives,
///     <see cref="Guid" />, date/time primitives, <see cref="JsonElement" />, <c>byte[]</c>,
///     lists/arrays of allowed values, and string-key dictionaries of allowed values.
///     Arbitrary CLR graphs (custom types, streams, delegates) are rejected at Put with
///     <c>checkpoint.unsupported_value_type</c>.
/// </remarks>
internal static class SqliteCheckpointJson
{
    private const int MaxDepth = 8;

    public static JsonElement? ToElement(object? value)
    {
        EnsureAllowed(value, "value", depth: 0);
        return value is null ? null : JsonSerializer.SerializeToElement(value, JsonSerializerOptions.Web);
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

    public static void EnsureAllowed(object? value, string path, int depth)
    {
        if (value is null)
        {
            return;
        }

        if (depth > MaxDepth)
        {
            throw new CheckpointStoreException(
                CheckpointWireFormat.UnsupportedValueTypeCode,
                $"Checkpoint wire format v1 rejects value at '{path}': nesting exceeds max depth {MaxDepth}.");
        }

        if (IsScalarAllowed(value))
        {
            return;
        }

        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not string key)
                {
                    throw new CheckpointStoreException(
                        CheckpointWireFormat.UnsupportedValueTypeCode,
                        $"Checkpoint wire format v1 requires string dictionary keys at '{path}' (got '{entry.Key?.GetType().FullName ?? "null"}').");
                }

                EnsureAllowed(entry.Value, $"{path}.{key}", depth + 1);
            }

            return;
        }

        if (value is IEnumerable enumerable and not string)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                EnsureAllowed(item, $"{path}[{index}]", depth + 1);
                index++;
            }

            return;
        }

        throw new CheckpointStoreException(
            CheckpointWireFormat.UnsupportedValueTypeCode,
            $"Checkpoint wire format v1 does not support value type '{value.GetType().FullName}' at '{path}'. "
            + "Use null, primitives, string, Guid, date/time, JsonElement, byte[], lists, or string-key dictionaries of those.");
    }

    private static bool IsScalarAllowed(object value)
    {
        return value is string
            or bool
            or char
            or byte
            or sbyte
            or short
            or ushort
            or int
            or uint
            or long
            or ulong
            or float
            or double
            or decimal
            or Guid
            or DateTime
            or DateTimeOffset
            or DateOnly
            or TimeOnly
            or TimeSpan
            or JsonElement
            or byte[];
    }
}
