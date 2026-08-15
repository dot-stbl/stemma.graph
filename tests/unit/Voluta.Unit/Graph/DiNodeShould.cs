using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Exceptions.Run;
using Voluta.Graph;
using Voluta.Graph.Builder;
using Voluta.Graph.Options;
using Xunit;

namespace Voluta.Unit.Graph;

public sealed class DiNodeShould
{
    [Fact(DisplayName = "Given AddNode<T> and CompileOptions.Services, when run, then node resolves from DI")]
    public async Task ResolveTypedNodeFromServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<EchoNode>();
        await using var provider = services.BuildServiceProvider();

        var graph = new StateGraph()
            .AddChannel("seed", ChannelKind.LastValue)
            .AddChannel("out", ChannelKind.LastValue)
            .AddNode<EchoNode>("echo")
            .AddEdge(GraphConstants.Start, "echo")
            .AddEdge("echo", GraphConstants.End)
            .Compile(
                new InMemoryCheckpointer(),
                new CompileOptions { Services = provider });

        var terminal = await graph.InvokeAsync(
            [new ChannelWrite("seed", "ping")],
            new RunOptions { ThreadId = "di-1", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        terminal.State.ShouldNotBeNull();
        terminal.State!["out"].ShouldBe("echo:ping");
    }

    [Fact(DisplayName = "Given AddNode without Services, when run, then fails with clear error")]
    public async Task FailWhenServicesMissing()
    {
        var graph = new StateGraph()
            .AddNode<EchoNode>("echo")
            .AddEdge(GraphConstants.Start, "echo")
            .AddEdge("echo", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var exception = await Should.ThrowAsync<GraphRunFailedException>(async () =>
            await graph.InvokeAsync(
                [],
                new RunOptions { ThreadId = "di-2", StreamMode = StreamMode.Values }));

        exception.Message.ShouldContain("GraphContext.Services is null");
    }

    private sealed class EchoNode : IGraphNode
    {
        public Task<NodeResult> InvokeAsync(GraphContext context, CancellationToken cancellationToken = default)
        {
            var seed = context.Read<string>("seed") ?? string.Empty;
            return Task.FromResult<NodeResult>(NodeResult.Continue(new ChannelWrite("out", $"echo:{seed}")));
        }
    }
}
