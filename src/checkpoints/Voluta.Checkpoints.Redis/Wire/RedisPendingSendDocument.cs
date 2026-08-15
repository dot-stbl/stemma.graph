using System.Text.Json;

namespace Voluta.Checkpoints.Redis.Wire;

/// <summary>
///     Wire shape for a pending Send entry.
/// </summary>
internal sealed class RedisPendingSendDocument
{
    public string NodeName { get; set; } = "";

    public string TaskId { get; set; } = "";

    public JsonElement? Payload { get; set; }
}
