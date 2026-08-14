using System.Text.Json;

namespace Voluta.Checkpoints.Postgres.Wire;

/// <summary>
///     Wire shape for one pending HITL interrupt.
/// </summary>
internal sealed class PostgresPendingInterruptDocument
{
    public string TaskId { get; set; } = "";

    public string NodeName { get; set; } = "";

    public JsonElement? Payload { get; set; }

    public JsonElement? TaskPayload { get; set; }
}
