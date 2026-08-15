using System.Text.Json;

namespace Voluta.Checkpoints.Redis.Wire;

/// <summary>
///     Wire shape for a pending write entry.
/// </summary>
internal sealed class RedisPendingWriteDocument
{
    public string TaskId { get; set; } = "";

    public string ChannelName { get; set; } = "";

    public JsonElement? Value { get; set; }
}
