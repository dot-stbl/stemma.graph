using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Abstractions.Checkpoint;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Streaming;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph;
using StemmaGraph.Graph.Builder;

namespace StemmaGraph.Benchmarks;

/// <summary>
///     Baselines for core runtime paths (not gated in CI).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class GraphRuntimeBenchmarks
{
    private CompiledGraph linear = null!;
    private CompiledGraph cycle = null!;
    private CompiledGraph parallelAppend = null!;
    private InMemoryCheckpointer checkpointer = null!;
    private long threadCounter;

    [GlobalSetup]
    public void Setup()
    {
        checkpointer = new InMemoryCheckpointer();

        linear = new StateGraph()
            .AddChannel("v", ChannelKind.LastValue)
            .AddNode(
                "a",
                static async (_, _) =>
                {
                    await Task.CompletedTask;
                    return NodeResult.Continue(new ChannelWrite("v", 1));
                })
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", GraphConstants.End)
            .Compile(checkpointer);

        cycle = new StateGraph()
            .AddChannel("n", ChannelKind.LastValue)
            .AddNode(
                "tick",
                static async (context, _) =>
                {
                    await Task.CompletedTask;
                    var n = context.Read<int>("n");
                    n++;
                    return NodeResult.Continue(new ChannelWrite("n", n));
                })
            .AddEdge(GraphConstants.Start, "tick")
            .AddConditionalEdges(
                "tick",
                static context => context.Read<int>("n") >= 5 ? GraphConstants.End : "tick")
            .Compile(checkpointer);

        parallelAppend = new StateGraph()
            .AddChannel("items", ChannelKind.Append)
            .AddNode(
                "left",
                static async (_, _) =>
                {
                    await Task.CompletedTask;
                    return NodeResult.Continue(new ChannelWrite("items", "L"));
                })
            .AddNode(
                "right",
                static async (_, _) =>
                {
                    await Task.CompletedTask;
                    return NodeResult.Continue(new ChannelWrite("items", "R"));
                })
            .AddEdge(GraphConstants.Start, "left")
            .AddEdge(GraphConstants.Start, "right")
            .AddEdge("left", GraphConstants.End)
            .AddEdge("right", GraphConstants.End)
            .Compile(checkpointer);
    }

    [Benchmark(Description = "Linear graph InvokeAsync (1 node)")]
    public async Task LinearInvokeAsync()
    {
        var id = $"lin-{Interlocked.Increment(ref threadCounter)}";
        await linear.InvokeAsync(
            [new ChannelWrite("v", 0)],
            new RunOptions { ThreadId = id, StreamMode = StreamMode.Values });
    }

    [Benchmark(Description = "Cycle 5 ticks InvokeAsync")]
    public async Task CycleFiveTicksAsync()
    {
        var id = $"cyc-{Interlocked.Increment(ref threadCounter)}";
        await cycle.InvokeAsync(
            [new ChannelWrite("n", 0)],
            new RunOptions { ThreadId = id, StreamMode = StreamMode.Values });
    }

    [Benchmark(Description = "Parallel ready + Append merge")]
    public async Task ParallelAppendAsync()
    {
        var id = $"par-{Interlocked.Increment(ref threadCounter)}";
        await parallelAppend.InvokeAsync(
            [],
            new RunOptions { ThreadId = id, StreamMode = StreamMode.Updates });
    }

    [Benchmark(Description = "InMemory Put+Get roundtrip")]
    public async Task CheckpointPutGetAsync()
    {
        var id = $"cp-{Interlocked.Increment(ref threadCounter)}";
        var snapshot = new CheckpointSnapshot
        {
            ThreadId = id,
            Step = 1,
            Status = GraphRunStatus.Running,
            ChannelValues = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["x"] = 42,
            },
            ChannelVersions = new Dictionary<string, long>(StringComparer.Ordinal) { ["x"] = 1 },
            VersionsSeen = new Dictionary<string, IReadOnlyDictionary<string, long>>(StringComparer.Ordinal),
            PendingWrites = [],
            NextNodes = ["a"],
        };

        await checkpointer.PutAsync(snapshot);
        await checkpointer.GetAsync(id);
    }
}
