using Shouldly;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Abstractions.State;
using StemmaGraph.Exceptions;
using StemmaGraph.Graph.Builder;
using Xunit;

namespace StemmaGraph.Unit.State;

public sealed class GraphChannelSchemaShould
{
    [Fact(DisplayName = "Given schema with two channels, when AddChannels is called, then both register without generator")]
    public void RegisterChannelsWithoutGenerator()
    {
        var schema = new GraphChannelSchema.Builder()
            .Add("messages", ChannelKind.Append)
            .Add("status", ChannelKind.LastValue)
            .Build();

        var graph = new StateGraph().AddChannels(schema);

        // Compile requires nodes + START edge — only assert AddChannels does not throw
        // and channels are available by re-adding a duplicate should fail.
        _ = Should.Throw<GraphCompileException>(() => graph.AddChannel("messages", ChannelKind.Append));
    }
}
