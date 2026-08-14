using System.Collections.Concurrent;
using System.Text.Json;
using Voluta.Abstractions.Checkpoint;
using Voluta.Checkpoints.File.Wire;

namespace Voluta.Checkpoints.File;

/// <summary>
///     JSON file checkpointer: one directory per thread, history as step-ordered files.
/// </summary>
/// <remarks>
///     Host registration: <c>v.Checkpoints.UseFile(rootDirectory)</c> (or
///     <c>AddVolutaCheckpoints(c =&gt; c.UseFile(...))</c>). Direct construction is
///     internal for conformance / unit tests only.
    ///     Channel values must be wire-format v1 allow-listed shapes (primitives, string,
    ///     lists/dictionaries of those, JsonElement). Unsupported types fail Put with
    ///     <c>checkpoint.unsupported_value_type</c>.
/// </remarks>
public sealed class FileCheckpointer : ICheckpointer
{
    private readonly ConcurrentDictionary<string, object> locks = new(StringComparer.Ordinal);
    private readonly string root;

    /// <summary>
    ///     Creates a file checkpointer rooted at <paramref name="rootDirectory" />.
    /// </summary>
    /// <param name="rootDirectory">Root directory for per-thread checkpoint JSON files.</param>
    internal FileCheckpointer(string rootDirectory)
    {
        root = InitRoot(rootDirectory);
    }

    /// <inheritdoc />
    public Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var gate = locks.GetOrAdd(snapshot.ThreadId, static _ => new object());
        lock (gate)
        {
            var threadDir = FileCheckpointPaths.ThreadDirectory(root, snapshot.ThreadId);
            Directory.CreateDirectory(threadDir);
            var path = Path.Combine(threadDir, $"{snapshot.Step:D12}.json");
            var document = FileCheckpointDocument.FromSnapshot(snapshot);
            var json = JsonSerializer.Serialize(document, JsonSerializerOptions.Web);
            System.IO.File.WriteAllText(path, json);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<CheckpointSnapshot?> GetAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var gate = locks.GetOrAdd(threadId, static _ => new object());
        lock (gate)
        {
            var threadDir = FileCheckpointPaths.ThreadDirectory(root, threadId);
            if (!Directory.Exists(threadDir))
            {
                return Task.FromResult<CheckpointSnapshot?>(null);
            }

            var latest = Directory.EnumerateFiles(threadDir, "*.json")
                .OrderByDescending(static path => path, StringComparer.Ordinal)
                .FirstOrDefault();
            if (latest is null)
            {
                return Task.FromResult<CheckpointSnapshot?>(null);
            }

            var json = System.IO.File.ReadAllText(latest);
            var document = JsonSerializer.Deserialize<FileCheckpointDocument>(json, JsonSerializerOptions.Web);
            return Task.FromResult(document?.ToSnapshot());
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CheckpointSnapshot>> ListAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var gate = locks.GetOrAdd(threadId, static _ => new object());
        lock (gate)
        {
            var threadDir = FileCheckpointPaths.ThreadDirectory(root, threadId);
            if (!Directory.Exists(threadDir))
            {
                return Task.FromResult<IReadOnlyList<CheckpointSnapshot>>([]);
            }

            var list = new List<CheckpointSnapshot>();
            foreach (var path in Directory.EnumerateFiles(threadDir, "*.json")
                         .OrderBy(static path => path, StringComparer.Ordinal))
            {
                var json = System.IO.File.ReadAllText(path);
                var document = JsonSerializer.Deserialize<FileCheckpointDocument>(json, JsonSerializerOptions.Web);
                if (document is not null)
                {
                    list.Add(document.ToSnapshot());
                }
            }

            return Task.FromResult<IReadOnlyList<CheckpointSnapshot>>(list);
        }
    }

    private static string InitRoot(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Root directory is required.", nameof(rootDirectory));
        }

        var full = Path.GetFullPath(rootDirectory);
        Directory.CreateDirectory(full);
        return full;
    }
}
