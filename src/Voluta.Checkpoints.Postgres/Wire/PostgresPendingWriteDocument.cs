using System.Text.Json;

namespace Voluta.Checkpoints.Postgres.Wire;

/// <summary>
///     Wire shape for a pending write entry.
/// </summary>
internal sealed class PostgresPendingWriteDocument
{
    public string TaskId { get; set; } = "";

    public string ChannelName { get; set; } = "";

    public JsonElement? Value { get; set; }
}
