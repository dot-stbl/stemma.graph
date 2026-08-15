using System.Text.Json;

namespace Voluta.Checkpoints.S3.Wire;

/// <summary>
///     Wire shape for a pending write entry.
/// </summary>
internal sealed class S3PendingWriteDocument
{
    public string TaskId { get; set; } = "";

    public string ChannelName { get; set; } = "";

    public JsonElement? Value { get; set; }
}
