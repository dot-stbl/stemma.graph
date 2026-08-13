using Voluta.Abstractions.Channels;

namespace Voluta.Runtime.Engine;

/// <summary>
///     One channel write attributed to a task (node) for deterministic apply order.
/// </summary>
internal sealed class TaskChannelWrite(string taskId, ChannelWrite write)
{
    public string TaskId { get; } = taskId;

    public ChannelWrite Write { get; } = write;
}
