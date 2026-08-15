using System.Text.Json;
using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Abstractions.Topology;
using Voluta.UI;
using Xunit;

namespace Voluta.Unit.UI;

public sealed class VolutaUiJsonWireShould
{
    [Fact(DisplayName = "Given topology, when ToWire, then serializes camelCase nodes and edges")]
    public void TopologyWire()
    {
        var topology = new GraphDescription
        {
            Nodes = ["a", "b"],
            Channels = new Dictionary<string, ChannelKind>(StringComparer.Ordinal)
            {
                ["messages"] = ChannelKind.Append,
            },
            StaticEdges = [new GraphEdgeDescription { Source = "__start__", Target = "a" }],
            ConditionalSources = ["a"],
            RecursionLimit = 16,
        };

        var json = JsonSerializer.Serialize(VolutaUiJson.ToWire(topology), JsonSerializerOptions.Web);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("nodes").GetArrayLength().ShouldBe(2);
        root.GetProperty("channels").GetProperty("messages").GetString().ShouldBe("Append");
        root.GetProperty("staticEdges")[0].GetProperty("source").GetString().ShouldBe("__start__");
        root.GetProperty("recursionLimit").GetInt32().ShouldBe(16);
    }

    [Fact(DisplayName = "Given thread snapshot, when ToWire, then serializes status and values")]
    public void ThreadSnapshotWire()
    {
        var state = new ThreadSnapshot
        {
            ThreadId = "t1",
            Step = 3,
            Status = GraphRunStatus.Interrupted,
            LastNode = "gate",
            Values = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["goal"] = "ship",
            },
        };

        var json = JsonSerializer.Serialize(VolutaUiJson.ToWire(state), JsonSerializerOptions.Web);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("threadId").GetString().ShouldBe("t1");
        root.GetProperty("status").GetString().ShouldBe("Interrupted");
        root.GetProperty("values").GetProperty("goal").GetString().ShouldBe("ship");
    }

    [Fact(DisplayName = "Given terminal stream event, when ToWireTerminal, then kind and step")]
    public void TerminalWire()
    {
        var terminal = new StreamEvent
        {
            Mode = StreamMode.Updates,
            Kind = StreamEventKind.End,
            Step = 9,
            NodeNames = ["notify"],
        };

        var json = JsonSerializer.Serialize(
            VolutaUiJson.ToWireTerminal(terminal),
            JsonSerializerOptions.Web);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("kind").GetString().ShouldBe("End");
        document.RootElement.GetProperty("step").GetInt64().ShouldBe(9);
    }
}
