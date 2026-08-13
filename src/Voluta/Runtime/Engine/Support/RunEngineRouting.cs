using Voluta.Graph;
using Voluta.Runtime.Engine.Tasks;

namespace Voluta.Runtime.Engine.Support;

/// <summary>
///     Ready-set routing helpers.
/// </summary>
internal static class RunEngineRouting
{
    public static IReadOnlyList<string> ResolveNextNodes(
        GraphTopology topology,
        string source,
        IReadOnlyDictionary<string, object?> channelValues,
        object? resumePayload)
    {
        if (topology.ConditionalEdges.TryGetValue(source, out var router))
        {
            var context = new GraphContext(source, channelValues, resumePayload);
            return [.. router(context)];
        }

        return topology.StaticEdges.TryGetValue(source, out var targets)
            ? [.. targets]
            : [];
    }

    public static IReadOnlyList<ReadyTask> ToPullTasks(GraphTopology topology, IReadOnlyList<string> candidates)
    {
        return
        [
            .. candidates
                .Where(name => name != GraphConstants.End && topology.Nodes.ContainsKey(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .Select(static name => new ReadyTask(name, name, null))
        ];
    }
}
