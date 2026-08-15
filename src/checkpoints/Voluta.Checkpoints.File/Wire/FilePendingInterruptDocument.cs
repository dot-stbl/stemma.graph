using System.Text.Json;

namespace Voluta.Checkpoints.File.Wire;

/// <summary>
///     Wire shape for one pending HITL interrupt.
/// </summary>
internal sealed class FilePendingInterruptDocument
{
    public string TaskId { get; set; } = "";

    public string NodeName { get; set; } = "";

    public JsonElement? Payload { get; set; }

    public JsonElement? TaskPayload { get; set; }
}
