using System.Collections.Concurrent;
using System.Text.Json;
using StemmaGraph.Abstractions.Checkpoint;
using StemmaGraph.Abstractions.Runtime;

namespace StemmaGraph.Checkpoints.File;

/// <summary>
///     JSON file checkpointer: one directory per thread, history as step-ordered files.
/// </summary>
/// <remarks>
///     Values are serialized with <see cref="JsonSerializer" />; prefer JSON-friendly types
///     (strings, numbers, lists of primitives). Complex CLR graphs may not roundtrip.
/// </remarks>
public sealed class FileCheckpointer : ICheckpointer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ConcurrentDictionary<string, object> locks = new(StringComparer.Ordinal);
    private readonly string rootDirectory;

    /// <summary>
    ///     Creates a checkpointer rooted at <paramref name="rootDirectory" /> (created if missing).
    /// </summary>
    /// <param name="rootDirectory">Directory for thread folders.</param>
    public FileCheckpointer(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Root directory is required.", nameof(rootDirectory));
        }

        this.rootDirectory = Path.GetFullPath(rootDirectory);
        Directory.CreateDirectory(this.rootDirectory);
    }

    /// <inheritdoc />
    public Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var gate = locks.GetOrAdd(snapshot.ThreadId, static _ => new object());
        lock (gate)
        {
            var threadDir = ThreadDirectory(snapshot.ThreadId);
            Directory.CreateDirectory(threadDir);
            var path = Path.Combine(threadDir, $"{snapshot.Step:D12}.json");
            var dto = FileCheckpointMapper.ToDto(snapshot);
            var json = JsonSerializer.Serialize(dto, JsonOptions);
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
            var threadDir = ThreadDirectory(threadId);
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
            var dto = JsonSerializer.Deserialize<FileCheckpointDto>(json, JsonOptions);
            return Task.FromResult(dto is null ? null : FileCheckpointMapper.FromDto(dto));
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
            var threadDir = ThreadDirectory(threadId);
            if (!Directory.Exists(threadDir))
            {
                return Task.FromResult<IReadOnlyList<CheckpointSnapshot>>([]);
            }

            var list = new List<CheckpointSnapshot>();
            foreach (var path in Directory.EnumerateFiles(threadDir, "*.json")
                         .OrderBy(static path => path, StringComparer.Ordinal))
            {
                var json = System.IO.File.ReadAllText(path);
                var dto = JsonSerializer.Deserialize<FileCheckpointDto>(json, JsonOptions);
                if (dto is not null)
                {
                    list.Add(FileCheckpointMapper.FromDto(dto));
                }
            }

            return Task.FromResult<IReadOnlyList<CheckpointSnapshot>>(list);
        }
    }

    private string ThreadDirectory(string threadId)
    {
        foreach (var ch in Path.GetInvalidFileNameChars())
        {
            threadId = threadId.Replace(ch, '_');
        }

        return Path.Combine(rootDirectory, threadId);
    }
}

internal sealed class FileCheckpointDto
{
    public int FormatVersion { get; set; } = 1;

    public string ThreadId { get; set; } = "";

    public long Step { get; set; }

    public string Status { get; set; } = nameof(GraphRunStatus.Running);

    public Dictionary<string, JsonElement?> ChannelValues { get; set; } = new(StringComparer.Ordinal);

    public Dictionary<string, long> ChannelVersions { get; set; } = new(StringComparer.Ordinal);

    public Dictionary<string, Dictionary<string, long>> VersionsSeen { get; set; } = new(StringComparer.Ordinal);

    public List<FilePendingWriteDto> PendingWrites { get; set; } = [];

    public List<FilePendingSendDto> PendingSends { get; set; } = [];

    public string? LastNode { get; set; }

    public List<string> NextNodes { get; set; } = [];

    public JsonElement? InterruptPayload { get; set; }
}

internal sealed class FilePendingWriteDto
{
    public string TaskId { get; set; } = "";

    public string ChannelName { get; set; } = "";

    public JsonElement? Value { get; set; }
}

internal sealed class FilePendingSendDto
{
    public string NodeName { get; set; } = "";

    public string TaskId { get; set; } = "";

    public JsonElement? Payload { get; set; }
}

file static class FileCheckpointMapper
{
    public static FileCheckpointDto ToDto(CheckpointSnapshot snapshot)
    {
        return new FileCheckpointDto
        {
            FormatVersion = snapshot.FormatVersion,
            ThreadId = snapshot.ThreadId,
            Step = snapshot.Step,
            Status = snapshot.Status.ToString(),
            ChannelValues = snapshot.ChannelValues.ToDictionary(
                static pair => pair.Key,
                static pair => ToElement(pair.Value),
                StringComparer.Ordinal),
            ChannelVersions = new Dictionary<string, long>(snapshot.ChannelVersions, StringComparer.Ordinal),
            VersionsSeen = snapshot.VersionsSeen.ToDictionary(
                static pair => pair.Key,
                static pair => new Dictionary<string, long>(pair.Value, StringComparer.Ordinal),
                StringComparer.Ordinal),
            PendingWrites =
            [
                .. snapshot.PendingWrites.Select(static write => new FilePendingWriteDto
                {
                    TaskId = write.TaskId,
                    ChannelName = write.ChannelName,
                    Value = ToElement(write.Value),
                })
            ],
            PendingSends =
            [
                .. snapshot.PendingSends.Select(static send => new FilePendingSendDto
                {
                    NodeName = send.NodeName,
                    TaskId = send.TaskId,
                    Payload = ToElement(send.Payload),
                })
            ],
            LastNode = snapshot.LastNode,
            NextNodes = [.. snapshot.NextNodes],
            InterruptPayload = ToElement(snapshot.InterruptPayload),
        };
    }

    public static CheckpointSnapshot FromDto(FileCheckpointDto dto)
    {
        var status = Enum.TryParse<GraphRunStatus>(dto.Status, ignoreCase: true, out var parsed)
            ? parsed
            : GraphRunStatus.Running;

        return new CheckpointSnapshot
        {
            FormatVersion = dto.FormatVersion,
            ThreadId = dto.ThreadId,
            Step = dto.Step,
            Status = status,
            ChannelValues = dto.ChannelValues.ToDictionary(
                static pair => pair.Key,
                static pair => FromElement(pair.Value),
                StringComparer.Ordinal),
            ChannelVersions = new Dictionary<string, long>(dto.ChannelVersions, StringComparer.Ordinal),
            VersionsSeen = dto.VersionsSeen.ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlyDictionary<string, long>)new Dictionary<string, long>(
                    pair.Value,
                    StringComparer.Ordinal),
                StringComparer.Ordinal),
            PendingWrites =
            [
                .. dto.PendingWrites.Select(static write => new PendingWrite
                {
                    TaskId = write.TaskId,
                    ChannelName = write.ChannelName,
                    Value = FromElement(write.Value),
                })
            ],
            PendingSends =
            [
                .. dto.PendingSends.Select(static send => new PendingSend
                {
                    NodeName = send.NodeName,
                    TaskId = send.TaskId,
                    Payload = FromElement(send.Payload),
                })
            ],
            LastNode = dto.LastNode,
            NextNodes = [.. dto.NextNodes],
            InterruptPayload = FromElement(dto.InterruptPayload),
        };
    }

    private static JsonElement? ToElement(object? value)
    {
        return value is null ? null : JsonSerializer.SerializeToElement(value);
    }

    private static object? FromElement(JsonElement? element)
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
