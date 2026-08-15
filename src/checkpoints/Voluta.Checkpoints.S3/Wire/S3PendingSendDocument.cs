using System.Text.Json;

namespace Voluta.Checkpoints.S3.Wire;

/// <summary>
///     Wire shape for a pending Send entry.
/// </summary>
internal sealed class S3PendingSendDocument
{
    public string NodeName { get; set; } = "";

    public string TaskId { get; set; } = "";

    public JsonElement? Payload { get; set; }
}
