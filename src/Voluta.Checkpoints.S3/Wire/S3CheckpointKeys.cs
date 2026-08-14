namespace Voluta.Checkpoints.S3.Wire;

/// <summary>
///     Object-key helpers for S3 checkpoint layout.
/// </summary>
internal static class S3CheckpointKeys
{
    private static readonly char[] InvalidKeyChars =
    [
        '\\', '{', '}', '^', '%', '`', ']', '"', '>', '[', '~', '<', '#', '|',
        '\0', '\r', '\n', '\t',
    ];

    public static string SanitizeThreadId(string threadId)
    {
        var safe = threadId;
        foreach (var ch in InvalidKeyChars)
        {
            safe = safe.Replace(ch, '_');
        }

        return safe.Replace(' ', '_');
    }

    public static string ThreadPrefix(string? keyPrefix, string threadId)
    {
        var safeThread = SanitizeThreadId(threadId);
        if (string.IsNullOrWhiteSpace(keyPrefix))
        {
            return safeThread + "/";
        }

        var trimmed = keyPrefix.Trim().Trim('/');
        return string.IsNullOrEmpty(trimmed) ? safeThread + "/" : trimmed + "/" + safeThread + "/";
    }

    public static string ObjectKey(string? keyPrefix, string threadId, long step)
    {
        return ThreadPrefix(keyPrefix, threadId) + $"{step:D12}.json";
    }

    public static bool TryParseStep(string objectKey, out long step)
    {
        step = 0;
        var fileName = objectKey;
        var slash = objectKey.LastIndexOf('/');
        if (slash >= 0 && slash < objectKey.Length - 1)
        {
            fileName = objectKey[(slash + 1)..];
        }

        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var stem = fileName[..^".json".Length];
        return long.TryParse(stem, out step);
    }
}
