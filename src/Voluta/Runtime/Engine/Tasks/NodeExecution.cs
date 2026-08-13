using Voluta.Abstractions.Results;

namespace Voluta.Runtime.Engine.Tasks;

/// <summary>
///     Node execution result pair for one superstep task.
/// </summary>
internal sealed class NodeExecution(string nodeName, string taskId, NodeResult result)
{
    public string NodeName { get; } = nodeName;

    public string TaskId { get; } = taskId;

    public NodeResult Result { get; } = result;
}
