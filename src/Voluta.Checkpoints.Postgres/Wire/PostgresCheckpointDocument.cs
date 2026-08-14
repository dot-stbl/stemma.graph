using System.Text.Json;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;

namespace Voluta.Checkpoints.Postgres.Wire;

/// <summary>
///     Wire shape for a single checkpoint JSON file.
/// </summary>
internal sealed class PostgresCheckpointDocument
{
    public int FormatVersion { get; set; } = 1;

    public string ThreadId { get; set; } = "";

    public long Step { get; set; }

    public string Status { get; set; } = nameof(GraphRunStatus.Running);

    public Dictionary<string, JsonElement?> ChannelValues { get; set; } = new(StringComparer.Ordinal);

    public Dictionary<string, long> ChannelVersions { get; set; } = new(StringComparer.Ordinal);

    public Dictionary<string, Dictionary<string, long>> VersionsSeen { get; set; } = new(StringComparer.Ordinal);

    public List<PostgresPendingWriteDocument> PendingWrites { get; set; } = [];

    public List<PostgresPendingSendDocument> PendingSends { get; set; } = [];

    public List<PostgresPendingInterruptDocument> PendingInterrupts { get; set; } = [];

    public string? LastNode { get; set; }

    public List<string> NextNodes { get; set; } = [];

    public JsonElement? InterruptPayload { get; set; }

    public static PostgresCheckpointDocument FromSnapshot(CheckpointSnapshot snapshot)
    {
        return new PostgresCheckpointDocument
        {
            FormatVersion = CheckpointWireFormat.Version,
            ThreadId = snapshot.ThreadId,
            Step = snapshot.Step,
            Status = snapshot.Status.ToString(),
            ChannelValues = snapshot.ChannelValues.ToDictionary(
                static pair => pair.Key,
                static pair => PostgresCheckpointJson.ToElement(pair.Value),
                StringComparer.Ordinal),
            ChannelVersions = new Dictionary<string, long>(snapshot.ChannelVersions, StringComparer.Ordinal),
            VersionsSeen = snapshot.VersionsSeen.ToDictionary(
                static pair => pair.Key,
                static pair => new Dictionary<string, long>(pair.Value, StringComparer.Ordinal),
                StringComparer.Ordinal),
            PendingWrites =
            [
                .. snapshot.PendingWrites.Select(static write => new PostgresPendingWriteDocument
                {
                    TaskId = write.TaskId,
                    ChannelName = write.ChannelName,
                    Value = PostgresCheckpointJson.ToElement(write.Value),
                })
            ],
            PendingSends =
            [
                .. snapshot.PendingSends.Select(static send => new PostgresPendingSendDocument
                {
                    NodeName = send.NodeName,
                    TaskId = send.TaskId,
                    Payload = PostgresCheckpointJson.ToElement(send.Payload),
                })
            ],
            PendingInterrupts =
            [
                .. snapshot.PendingInterrupts.Select(static item => new PostgresPendingInterruptDocument
                {
                    TaskId = item.TaskId,
                    NodeName = item.NodeName,
                    Payload = PostgresCheckpointJson.ToElement(item.Payload),
                    TaskPayload = PostgresCheckpointJson.ToElement(item.TaskPayload),
                })
            ],
            LastNode = snapshot.LastNode,
            NextNodes = [.. snapshot.NextNodes],
            InterruptPayload = PostgresCheckpointJson.ToElement(snapshot.InterruptPayload),
        };
    }

    public CheckpointSnapshot ToSnapshot()
    {
        CheckpointWireFormat.EnsureSupported(FormatVersion);

        var status = Enum.TryParse<GraphRunStatus>(Status, ignoreCase: true, out var parsed)
            ? parsed
            : GraphRunStatus.Running;

        return new CheckpointSnapshot
        {
            FormatVersion = FormatVersion,
            ThreadId = ThreadId,
            Step = Step,
            Status = status,
            ChannelValues = ChannelValues.ToDictionary(
                static pair => pair.Key,
                static pair => PostgresCheckpointJson.FromElement(pair.Value),
                StringComparer.Ordinal),
            ChannelVersions = new Dictionary<string, long>(ChannelVersions, StringComparer.Ordinal),
            VersionsSeen = VersionsSeen.ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlyDictionary<string, long>)new Dictionary<string, long>(
                    pair.Value,
                    StringComparer.Ordinal),
                StringComparer.Ordinal),
            PendingWrites =
            [
                .. PendingWrites.Select(static write => new PendingWrite
                {
                    TaskId = write.TaskId,
                    ChannelName = write.ChannelName,
                    Value = PostgresCheckpointJson.FromElement(write.Value),
                })
            ],
            PendingSends =
            [
                .. PendingSends.Select(static send => new PendingSend
                {
                    NodeName = send.NodeName,
                    TaskId = send.TaskId,
                    Payload = PostgresCheckpointJson.FromElement(send.Payload),
                })
            ],
            PendingInterrupts =
            [
                .. PendingInterrupts.Select(static item => new PendingInterrupt
                {
                    TaskId = item.TaskId,
                    NodeName = item.NodeName,
                    Payload = PostgresCheckpointJson.FromElement(item.Payload),
                    TaskPayload = PostgresCheckpointJson.FromElement(item.TaskPayload),
                })
            ],
            LastNode = LastNode,
            NextNodes = [.. NextNodes],
            InterruptPayload = PostgresCheckpointJson.FromElement(InterruptPayload),
        };
    }
}
