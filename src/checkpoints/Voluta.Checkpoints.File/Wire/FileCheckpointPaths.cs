namespace Voluta.Checkpoints.File.Wire;

/// <summary>
///     Path helpers for file checkpoint layout.
/// </summary>
internal static class FileCheckpointPaths
{
    public static string ThreadDirectory(string rootDirectory, string threadId)
    {
        var safe = threadId;
        foreach (var ch in Path.GetInvalidFileNameChars())
        {
            safe = safe.Replace(ch, '_');
        }

        return Path.Combine(rootDirectory, safe);
    }
}
