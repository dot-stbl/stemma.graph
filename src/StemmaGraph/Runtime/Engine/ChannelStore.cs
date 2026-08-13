using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Channels;
using StemmaGraph.Exceptions;
using StemmaGraph.Exceptions.Run;

namespace StemmaGraph.Runtime.Engine;

/// <summary>
///     Mutable channel map + versions for one run thread.
/// </summary>
internal sealed class ChannelStore
{
    private readonly Dictionary<string, IChannel> channels = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> versions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, long>> versionsSeen = new(StringComparer.Ordinal);

    /// <summary>
    ///     Creates channels for the declared topology.
    /// </summary>
    public ChannelStore(IReadOnlyDictionary<string, ChannelKind> declarations)
    {
        foreach (var (name, kind) in declarations)
        {
            channels[name] = ChannelFactory.Create(kind);
            versions[name] = 0;
        }
    }

    /// <summary>
    ///     Current channel versions.
    /// </summary>
    public IReadOnlyDictionary<string, long> Versions => versions;

    /// <summary>
    ///     Per-node versions_seen map.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, long>> VersionsSeen =>
        versionsSeen.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyDictionary<string, long>)new Dictionary<string, long>(
                pair.Value,
                StringComparer.Ordinal),
            StringComparer.Ordinal);

    /// <summary>
    ///     Snapshots current channel values.
    /// </summary>
    public IReadOnlyDictionary<string, object?> SnapshotValues()
    {
        return channels.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.Get(),
            StringComparer.Ordinal);
    }

    /// <summary>
    ///     Restores values, versions, and versions_seen from a checkpoint.
    /// </summary>
    public void Restore(
        IReadOnlyDictionary<string, object?> values,
        IReadOnlyDictionary<string, long> channelVersions,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, long>> seen)
    {
        foreach (var (name, channel) in channels)
        {
            channel.Restore(values.TryGetValue(name, out var value) ? value : null);
        }

        versions.Clear();
        foreach (var (name, version) in channelVersions)
        {
            versions[name] = version;
        }

        foreach (var name in channels.Keys)
        {
            _ = versions.TryAdd(name, 0);
        }

        versionsSeen.Clear();
        foreach (var (nodeName, map) in seen)
        {
            versionsSeen[nodeName] = new Dictionary<string, long>(map, StringComparer.Ordinal);
        }
    }

    /// <summary>
    ///     Seeds initial input writes before the first superstep (as step 0 apply).
    /// </summary>
    public void ApplyInputWrites(IEnumerable<ChannelWrite> writes)
    {
        ApplyGrouped(ChannelWriteGrouping.Group(writes));
    }

    /// <summary>
    ///     Applies node writes for a superstep in deterministic channel/task order.
    /// </summary>
    public void ApplyWrites(IReadOnlyList<TaskChannelWrite> writes)
    {
        var ordered = writes
            .OrderBy(static item => item.Write.ChannelName, StringComparer.Ordinal)
            .ThenBy(static item => item.TaskId, StringComparer.Ordinal)
            .ToList();

        var grouped = new Dictionary<string, List<object?>>(StringComparer.Ordinal);
        foreach (var item in ordered)
        {
            var write = item.Write;
            if (!grouped.TryGetValue(write.ChannelName, out var list))
            {
                list = [];
                grouped[write.ChannelName] = list;
            }

            list.Add(write.Value);
        }

        ApplyGrouped(grouped);
    }

    /// <summary>
    ///     Marks channels as seen by a node after it ran.
    /// </summary>
    public void MarkSeen(string nodeName)
    {
        if (!versionsSeen.TryGetValue(nodeName, out var map))
        {
            map = new Dictionary<string, long>(StringComparer.Ordinal);
            versionsSeen[nodeName] = map;
        }

        foreach (var (channelName, version) in versions)
        {
            map[channelName] = version;
        }
    }

    private void ApplyGrouped(IReadOnlyDictionary<string, List<object?>> grouped)
    {
        foreach (var (channelName, values) in grouped.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            if (!channels.TryGetValue(channelName, out var channel))
            {
                throw new GraphRunFailedException(
                    $"Write targeted undeclared channel '{channelName}'.");
            }

            try
            {
                channel.Update(values);
            }
            catch (GraphConcurrentUpdateException exception)
            {
                throw new GraphConcurrentUpdateException(
                    $"Channel '{channelName}': {exception.Message}");
            }

            versions[channelName] = versions.GetValueOrDefault(channelName) + 1;
        }
    }
}

/// <summary>
///     Groups channel writes by name in deterministic order.
/// </summary>
file static class ChannelWriteGrouping
{
    public static Dictionary<string, List<object?>> Group(IEnumerable<ChannelWrite> writes)
    {
        var grouped = new Dictionary<string, List<object?>>(StringComparer.Ordinal);
        foreach (var write in writes.OrderBy(static item => item.ChannelName, StringComparer.Ordinal))
        {
            if (!grouped.TryGetValue(write.ChannelName, out var list))
            {
                list = [];
                grouped[write.ChannelName] = list;
            }

            list.Add(write.Value);
        }

        return grouped;
    }
}
