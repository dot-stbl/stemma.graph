using System.Text.Json;

namespace Voluta.Checkpoints.Sqlite.Wire;

/// <summary>
///     Wire shape for a pending write entry.
/// </summary>
internal sealed class SqlitePendingWriteDocument
{
    public string TaskId { get; set; } = "";

    public string ChannelName { get; set; } = "";

    public JsonElement? Value { get; set; }
}
