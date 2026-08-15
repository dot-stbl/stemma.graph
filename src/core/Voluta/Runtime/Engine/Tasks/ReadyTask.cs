namespace Voluta.Runtime.Engine.Tasks;

/// <summary>
///     One ready task for a superstep (PULL edge or PUSH/Send).
/// </summary>
internal sealed class ReadyTask(string nodeName, string taskId, object? taskPayload)
{
    public string NodeName { get; } = nodeName;

    public string TaskId { get; } = taskId;

    public object? TaskPayload { get; } = taskPayload;
}
